using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Imports per-country product price overrides from a CSV. One file per
    /// country: columns SKU, Price. Existing prices for the same (Product,
    /// Country) get overwritten; new ones get inserted. Rows for unknown
    /// SKUs are flagged but skipped.
    /// </summary>
    public class CountryPriceImportService
    {
        private readonly ApplicationDbContext _context;

        public CountryPriceImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public static readonly string[] RequiredHeaders = { "SKU", "Price" };

        public enum RowStatus { WillInsert, WillUpdate, Invalid, UnknownSku }

        public class ParsedRow
        {
            public int LineNumber { get; set; }
            public RowStatus Status { get; set; }
            public List<string> Errors { get; set; } = new();
            public string? SKU { get; set; }
            public decimal Price { get; set; }
            public int? ProductId { get; set; }   // resolved at parse time
            public int? ExistingPriceId { get; set; } // resolved at parse time
        }

        public class ImportResult
        {
            public string CountryCode { get; set; } = "";
            public string CountryName { get; set; } = "";
            public string CurrencyCode { get; set; } = "";
            public List<ParsedRow> Rows { get; set; } = new();
            public List<string> FileErrors { get; set; } = new();

            public int WillInsertCount => Rows.Count(r => r.Status == RowStatus.WillInsert);
            public int WillUpdateCount => Rows.Count(r => r.Status == RowStatus.WillUpdate);
            public int InvalidCount    => Rows.Count(r => r.Status == RowStatus.Invalid);
            public int UnknownSkuCount => Rows.Count(r => r.Status == RowStatus.UnknownSku);
            public bool HasFileErrors  => FileErrors.Count > 0;
        }

        public async Task<ImportResult> ParseAndValidateAsync(string csvContent, string countryCode)
        {
            var result = new ImportResult { CountryCode = countryCode };

            var country = await _context.Countries.FindAsync(countryCode);
            if (country is null || !country.IsActive)
            {
                result.FileErrors.Add($"Country '{countryCode}' is not active or does not exist.");
                return result;
            }
            result.CountryName = country.Name;
            result.CurrencyCode = country.CurrencyCode;

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                result.FileErrors.Add("The uploaded file is empty.");
                return result;
            }
            if (csvContent.Length > 0 && csvContent[0] == '﻿') csvContent = csvContent[1..];

            var lines = SplitCsvLines(csvContent);
            if (lines.Count < 1)
            {
                result.FileErrors.Add("No rows found.");
                return result;
            }

            var header = ParseCsvLine(lines[0]);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
            {
                var h = header[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h)) headerMap[h] = i;
            }
            foreach (var required in RequiredHeaders)
            {
                if (!headerMap.ContainsKey(required))
                    result.FileErrors.Add($"Missing required column: '{required}'.");
            }
            if (result.HasFileErrors) return result;

            // Pre-load product Id-by-SKU and existing ProductPrice rows for this country.
            var products = await _context.Products
                .Select(p => new { p.Id, p.SKU })
                .ToListAsync();
            var productBySku = products.ToDictionary(p => p.SKU, p => p.Id, StringComparer.OrdinalIgnoreCase);

            var existingForCountry = await _context.ProductPrices
                .Where(pp => pp.CountryCode == countryCode)
                .Select(pp => new { pp.Id, pp.ProductId })
                .ToListAsync();
            var existingByProductId = existingForCountry.ToDictionary(e => e.ProductId, e => e.Id);

            var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                var priceStr = Get("Price");

                if (string.IsNullOrWhiteSpace(row.SKU))
                    row.Errors.Add("SKU is required.");
                else if (!seenSkus.Add(row.SKU))
                    row.Errors.Add($"SKU '{row.SKU}' appears more than once in this file.");

                decimal price = 0m;
                if (string.IsNullOrWhiteSpace(priceStr))
                    row.Errors.Add("Price is required.");
                else if (!TryParseDecimal(priceStr, out price) || price < 0)
                    row.Errors.Add($"Price '{priceStr}' is not a valid non-negative number.");

                if (row.Errors.Count > 0)
                {
                    row.Status = RowStatus.Invalid;
                    result.Rows.Add(row);
                    continue;
                }

                if (!productBySku.TryGetValue(row.SKU!, out var pid))
                {
                    row.Status = RowStatus.UnknownSku;
                    row.Errors.Add($"SKU '{row.SKU}' does not exist in the catalogue.");
                    result.Rows.Add(row);
                    continue;
                }

                row.ProductId = pid;
                row.Price = price;
                row.Status = existingByProductId.TryGetValue(pid, out var existingId)
                    ? (row.ExistingPriceId = existingId).HasValue ? RowStatus.WillUpdate : RowStatus.WillInsert
                    : RowStatus.WillInsert;

                result.Rows.Add(row);
            }

            return result;
        }

        public async Task<(int inserted, int updated)> CommitAsync(ImportResult validated, string? userId)
        {
            int inserted = 0, updated = 0;
            foreach (var r in validated.Rows.Where(r =>
                r.Status == RowStatus.WillInsert || r.Status == RowStatus.WillUpdate))
            {
                if (r.ProductId is null) continue;

                if (r.Status == RowStatus.WillUpdate && r.ExistingPriceId.HasValue)
                {
                    var existing = await _context.ProductPrices.FindAsync(r.ExistingPriceId.Value);
                    if (existing != null)
                    {
                        existing.Price = r.Price;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.UpdatedByUserId = userId;
                        updated++;
                    }
                }
                else
                {
                    _context.ProductPrices.Add(new ProductPrice
                    {
                        ProductId = r.ProductId.Value,
                        CountryCode = validated.CountryCode,
                        Price = r.Price,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedByUserId = userId
                    });
                    inserted++;
                }
            }

            await _context.SaveChangesAsync();
            return (inserted, updated);
        }

        public static string BuildTemplateCsv(string countryCode, string currencyCode)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# ShumoShop price list for {countryCode} ({currencyCode}).");
            sb.AppendLine("# Replace the example rows below with real SKUs from your catalogue.");
            sb.AppendLine("SKU,Price");
            sb.AppendLine("M42,12.50");
            sb.AppendLine("M42-PK25,55.00");
            return sb.ToString();
        }

        // ── CSV parsing helpers — same as ProductCsvImportService ──────

        private static bool TryParseDecimal(string s, out decimal value)
        {
            if (string.IsNullOrWhiteSpace(s)) { value = 0m; return false; }
            var cleaned = s.Trim()
                .Replace("R", "", StringComparison.OrdinalIgnoreCase)
                .Replace("$", "", StringComparison.Ordinal)
                .Replace("£", "", StringComparison.Ordinal)
                .Replace("€", "", StringComparison.Ordinal)
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
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"') { sb.Append('"'); i++; continue; }
                    inQuotes = !inQuotes;
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                    var line = sb.ToString();
                    if (!line.TrimStart().StartsWith("#")) lines.Add(line); // skip #comments
                    sb.Clear();
                }
                else sb.Append(c);
            }
            if (sb.Length > 0)
            {
                var line = sb.ToString();
                if (!line.TrimStart().StartsWith("#")) lines.Add(line);
            }
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
                    if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                    else if (c == '"') inQuotes = true;
                    else sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
        }
    }
}
