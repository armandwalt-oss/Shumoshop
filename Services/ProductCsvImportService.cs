using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Parses, validates and imports products from a CSV file.
    /// Ported from CXI's ProductCsvImportService and adapted to ShumoShop's
    /// Product model (no variants / customisation fields).
    ///
    /// SAFETY GUARANTEES:
    ///   • Import is IDEMPOTENT by SKU — existing products are NEVER updated,
    ///     overwritten or deleted. Only rows whose SKU does not already exist
    ///     are inserted.
    ///   • Categories and subcategories are resolved by Name (case-insensitive).
    ///     Missing categories flag the row as an error, never auto-created.
    ///   • Validation runs BEFORE any database writes. The admin can still
    ///     commit a file with errors — invalid rows are skipped, valid rows
    ///     are inserted.
    ///   • All inserts happen inside a single SaveChangesAsync() call so the
    ///     import is atomic.
    /// </summary>
    public class ProductCsvImportService
    {
        private readonly ApplicationDbContext _context;

        public ProductCsvImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Column names (header must match, case-insensitive) ───────────────
        public static readonly string[] RequiredHeaders = { "SKU", "Name", "Price", "CategoryName" };

        public static readonly string[] AllHeaders =
        {
            "SKU", "Name", "Description", "Price",
            "CategoryName", "SubCategoryName",
            "StockQuantity", "ImageUrl",
            "IsFeatured", "IsNewArrival", "IsSpecial"
        };

        // ── Row-level status ─────────────────────────────────────────────────
        public enum RowStatus
        {
            WillInsert,        // valid + SKU does not exist → will be inserted
            WillSkipDuplicate, // valid but SKU already exists → left untouched
            Invalid            // has validation errors → not imported
        }

        public class ParsedRow
        {
            public int LineNumber { get; set; }
            public RowStatus Status { get; set; }
            public List<string> Errors { get; set; } = new();
            public Product? Product { get; set; }

            // Raw values shown in preview for context
            public string? SKU { get; set; }
            public string? Name { get; set; }
            public string? CategoryName { get; set; }
            public string? Price { get; set; }
        }

        public class ImportResult
        {
            public List<ParsedRow> Rows { get; set; } = new();
            public List<string> FileErrors { get; set; } = new();

            public int WillInsertCount        => Rows.Count(r => r.Status == RowStatus.WillInsert);
            public int WillSkipDuplicateCount => Rows.Count(r => r.Status == RowStatus.WillSkipDuplicate);
            public int InvalidCount           => Rows.Count(r => r.Status == RowStatus.Invalid);
            public bool HasFileErrors         => FileErrors.Count > 0;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Parse and validate a CSV. Touches the DB only for reads (categories
        /// + existing SKUs). Safe to call repeatedly for preview.
        /// </summary>
        public async Task<ImportResult> ParseAndValidateAsync(string csvContent)
        {
            var result = new ImportResult();

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                result.FileErrors.Add("The uploaded file is empty.");
                return result;
            }

            // Strip BOM if present
            if (csvContent.Length > 0 && csvContent[0] == '﻿')
                csvContent = csvContent[1..];

            var lines = SplitCsvLines(csvContent);
            if (lines.Count < 1)
            {
                result.FileErrors.Add("No rows found in the file.");
                return result;
            }

            // Header
            var header = ParseCsvLine(lines[0]);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                var h = header[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
                    headerMap[h] = i;
            }

            foreach (var required in RequiredHeaders)
            {
                if (!headerMap.ContainsKey(required))
                    result.FileErrors.Add($"Missing required column: '{required}'.");
            }
            if (result.HasFileErrors) return result;

            // Lookups
            var categories = await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            var categoryByName = categories.ToDictionary(
                c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

            var subcategories = await _context.SubCategories
                .Select(s => new { s.Id, s.Name, s.CategoryId })
                .ToListAsync();

            var existingSkus = await _context.Products
                .Select(p => p.SKU)
                .ToListAsync();
            var existingSkuSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

            var seenSkusInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;

            for (int i = 1; i < lines.Count; i++)
            {
                var raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var fields = ParseCsvLine(raw);
                var row = new ParsedRow { LineNumber = i + 1 };

                string Get(string col) =>
                    headerMap.TryGetValue(col, out var idx) && idx < fields.Count
                        ? fields[idx]?.Trim() ?? ""
                        : "";

                row.SKU = Get("SKU");
                row.Name = Get("Name");
                row.CategoryName = Get("CategoryName");
                row.Price = Get("Price");

                // Required fields
                if (string.IsNullOrWhiteSpace(row.SKU))
                    row.Errors.Add("SKU is required.");
                if (string.IsNullOrWhiteSpace(row.Name))
                    row.Errors.Add("Name is required.");
                if (string.IsNullOrWhiteSpace(row.CategoryName))
                    row.Errors.Add("CategoryName is required.");
                if (string.IsNullOrWhiteSpace(row.Price))
                    row.Errors.Add("Price is required.");

                // Duplicate SKU within the file
                if (!string.IsNullOrWhiteSpace(row.SKU) && !seenSkusInFile.Add(row.SKU))
                    row.Errors.Add($"SKU '{row.SKU}' appears more than once in this file.");

                // Category resolve
                int? categoryId = null;
                if (!string.IsNullOrWhiteSpace(row.CategoryName))
                {
                    if (categoryByName.TryGetValue(row.CategoryName, out var catId))
                        categoryId = catId;
                    else
                        row.Errors.Add($"Category '{row.CategoryName}' does not exist. Create it first.");
                }

                // SubCategory resolve (optional). Must belong to the resolved Category.
                int? subCategoryId = null;
                var subCatName = Get("SubCategoryName");
                if (!string.IsNullOrWhiteSpace(subCatName) && categoryId.HasValue)
                {
                    var match = subcategories.FirstOrDefault(s =>
                        s.CategoryId == categoryId.Value &&
                        string.Equals(s.Name, subCatName, StringComparison.OrdinalIgnoreCase));

                    if (match is null)
                        row.Errors.Add($"SubCategory '{subCatName}' does not exist under category '{row.CategoryName}'.");
                    else
                        subCategoryId = match.Id;
                }

                // Numeric parsing
                decimal price = 0m;
                if (!string.IsNullOrWhiteSpace(row.Price))
                {
                    if (!TryParseDecimal(row.Price, out price) || price < 0)
                        row.Errors.Add($"Price '{row.Price}' is not a valid non-negative number.");
                }

                int stockQty = 0;
                var sq = Get("StockQuantity");
                if (!string.IsNullOrWhiteSpace(sq))
                {
                    if (!int.TryParse(sq, NumberStyles.Integer, CultureInfo.InvariantCulture, out stockQty) || stockQty < 0)
                        row.Errors.Add($"StockQuantity '{sq}' is not a valid non-negative integer.");
                }

                bool isFeatured   = TryParseBool(Get("IsFeatured"),   false);
                bool isNewArrival = TryParseBool(Get("IsNewArrival"), false);
                bool isSpecial    = TryParseBool(Get("IsSpecial"),    false);

                // Decide final status
                if (row.Errors.Count > 0)
                {
                    row.Status = RowStatus.Invalid;
                }
                else if (!string.IsNullOrWhiteSpace(row.SKU) && existingSkuSet.Contains(row.SKU))
                {
                    row.Status = RowStatus.WillSkipDuplicate;
                }
                else
                {
                    row.Status = RowStatus.WillInsert;
                    row.Product = new Product
                    {
                        SKU = row.SKU!,
                        Name = row.Name!,
                        Description = NullIfEmpty(Get("Description")) ?? "",
                        Price = price,
                        CategoryId = categoryId!.Value,
                        SubCategoryId = subCategoryId,
                        ImageUrl = NullIfEmpty(Get("ImageUrl")) ?? "",
                        StockQuantity = stockQty,
                        InStock = stockQty > 0,
                        IsFeatured = isFeatured,
                        IsNewArrival = isNewArrival,
                        IsSpecial = isSpecial,
                        CreatedDate = now
                    };
                }

                result.Rows.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Commit the WillInsert rows. Invalid + duplicate rows are skipped.
        /// Re-checks SKUs at commit time as a defence-in-depth measure.
        /// </summary>
        public async Task<int> CommitAsync(ImportResult validated)
        {
            var toInsert = validated.Rows
                .Where(r => r.Status == RowStatus.WillInsert && r.Product != null)
                .Select(r => r.Product!)
                .ToList();

            if (toInsert.Count == 0) return 0;

            var skus = toInsert.Select(p => p.SKU).ToList();
            var existingSkus = await _context.Products
                .Where(p => skus.Contains(p.SKU))
                .Select(p => p.SKU)
                .ToListAsync();
            var existingSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

            var trulyNew = toInsert.Where(p => !existingSet.Contains(p.SKU)).ToList();
            if (trulyNew.Count == 0) return 0;

            await _context.Products.AddRangeAsync(trulyNew);
            await _context.SaveChangesAsync();
            return trulyNew.Count;
        }

        /// <summary>
        /// Build a CSV template the admin can download as a starting point.
        /// </summary>
        public static string BuildTemplateCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", AllHeaders));

            sb.AppendLine(Csv(
                "EXAMPLE-CLIP-01", "Example Push-Type Retainer",
                "Pack of 25 push-type retainers.",
                "49.99",
                "Automotive Clips & Fasteners", "Push Type Retainers",
                "100", "",
                "true", "false", "false"));

            sb.AppendLine(Csv(
                "EXAMPLE-RIV-01", "Example Fir Tree Rivet",
                "Pack of 50 fir tree rivets.",
                "35.00",
                "Automotive Clips & Fasteners", "Fir Tree Rivet",
                "200", "",
                "false", "true", "false"));

            return sb.ToString();
        }

        // ── Internals ────────────────────────────────────────────────────────

        private static string? NullIfEmpty(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static bool TryParseBool(string s, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            s = s.Trim();
            if (s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("yes",  StringComparison.OrdinalIgnoreCase) ||
                s.Equals("y",    StringComparison.OrdinalIgnoreCase) ||
                s.Equals("1",    StringComparison.OrdinalIgnoreCase))
                return true;
            if (s.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("no",    StringComparison.OrdinalIgnoreCase) ||
                s.Equals("n",     StringComparison.OrdinalIgnoreCase) ||
                s.Equals("0",     StringComparison.OrdinalIgnoreCase))
                return false;
            return defaultValue;
        }

        private static bool TryParseDecimal(string s, out decimal value)
        {
            if (string.IsNullOrWhiteSpace(s)) { value = 0m; return false; }
            var cleaned = s.Trim()
                .Replace("R", "", StringComparison.OrdinalIgnoreCase)
                .Replace("$", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal);

            if (cleaned.Contains(',') && !cleaned.Contains('.'))
                cleaned = cleaned.Replace(',', '.');

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static List<string> SplitCsvLines(string csv)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(csv)) return lines;

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (c == '"')
                {
                    sb.Append(c);
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                        continue;
                    }
                    inQuotes = !inQuotes;
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0) lines.Add(sb.ToString());
            return lines;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == ',')        { result.Add(sb.ToString()); sb.Clear(); }
                    else if (c == '"')   { inQuotes = true; }
                    else                 { sb.Append(c); }
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        private static string Csv(params string[] fields) =>
            string.Join(",", fields.Select(EscapeField));

        private static string EscapeField(string s)
        {
            if (s == null) return "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
