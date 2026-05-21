using System.Text.RegularExpressions;

namespace WebApplication1.Helpers
{
    /// <summary>
    /// Validation helpers for South African specific data
    /// </summary>
    public static class SouthAfricanValidation
    {
        /// <summary>
        /// South African provinces
        /// </summary>
        public static readonly string[] Provinces =
        {
            "Eastern Cape",
            "Free State",
            "Gauteng",
            "KwaZulu-Natal",
            "Limpopo",
            "Mpumalanga",
            "Northern Cape",
            "North West",
            "Western Cape"
        };

        /// <summary>
        /// Validates South African postal code (4 digits)
        /// </summary>
        public static bool IsValidPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
                return false;

            // SA postal codes are exactly 4 digits
            return Regex.IsMatch(postalCode.Trim(), @"^\d{4}$");
        }

        /// <summary>
        /// Validates South African mobile phone number
        /// Accepts formats: 0821234567, +27821234567, 082 123 4567, +27 82 123 4567
        /// </summary>
        public static bool IsValidMobileNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove spaces, dashes, and parentheses
            string cleaned = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

            // Pattern 1: 0821234567 (10 digits starting with 0)
            if (Regex.IsMatch(cleaned, @"^0[6-8]\d{8}$"))
                return true;

            // Pattern 2: +27821234567 (12 digits starting with +27)
            if (Regex.IsMatch(cleaned, @"^\+27[6-8]\d{8}$"))
                return true;

            // Pattern 3: 27821234567 (11 digits starting with 27)
            if (Regex.IsMatch(cleaned, @"^27[6-8]\d{8}$"))
                return true;

            return false;
        }

        /// <summary>
        /// Validates South African landline number
        /// </summary>
        public static bool IsValidLandlineNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove spaces, dashes, and parentheses
            string cleaned = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

            // Landline: 0211234567 (10 digits starting with 0)
            if (Regex.IsMatch(cleaned, @"^0[1-5]\d{8}$"))
                return true;

            // International format
            if (Regex.IsMatch(cleaned, @"^\+27[1-5]\d{8}$"))
                return true;

            return false;
        }

        /// <summary>
        /// Formats phone number to standard SA format
        /// </summary>
        public static string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Remove all non-digit characters except +
            string cleaned = Regex.Replace(phoneNumber, @"[^\d\+]", "");

            // If starts with +27, format as +27 82 123 4567
            if (cleaned.StartsWith("+27") && cleaned.Length == 12)
            {
                return $"+27 {cleaned.Substring(3, 2)} {cleaned.Substring(5, 3)} {cleaned.Substring(8, 4)}";
            }

            // If starts with 0 and is 10 digits, format as 082 123 4567
            if (cleaned.StartsWith("0") && cleaned.Length == 10)
            {
                return $"{cleaned.Substring(0, 3)} {cleaned.Substring(3, 3)} {cleaned.Substring(6, 4)}";
            }

            return phoneNumber; // Return original if doesn't match patterns
        }

        /// <summary>
        /// Validates South African ID number (basic validation)
        /// </summary>
        public static bool IsValidIdNumber(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;

            // Remove spaces
            string cleaned = idNumber.Replace(" ", "");

            // SA ID numbers are 13 digits
            if (!Regex.IsMatch(cleaned, @"^\d{13}$"))
                return false;

            // Basic date validation (YYMMDD)
            if (!int.TryParse(cleaned.Substring(0, 2), out int year))
                return false;

            if (!int.TryParse(cleaned.Substring(2, 2), out int month))
                return false;

            if (!int.TryParse(cleaned.Substring(4, 2), out int day))
                return false;

            // Month must be 01-12
            if (month < 1 || month > 12)
                return false;

            // Day must be 01-31
            if (day < 1 || day > 31)
                return false;

            return true;
        }

        /// <summary>
        /// Validates that a province is a valid South African province
        /// </summary>
        public static bool IsValidProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return false;

            return Provinces.Contains(province.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets major cities for a given province
        /// </summary>
        public static string[] GetMajorCitiesForProvince(string province)
        {
            return province?.Trim().ToLower() switch
            {
                "gauteng" => new[] { "Johannesburg", "Pretoria", "Midrand", "Sandton", "Randburg", "Roodepoort", "Centurion", "Kempton Park" },
                "western cape" => new[] { "Cape Town", "Stellenbosch", "Paarl", "Somerset West", "George", "Hermanus" },
                "kwazulu-natal" => new[] { "Durban", "Pietermaritzburg", "Ballito", "Umhlanga", "Newcastle", "Richards Bay" },
                "eastern cape" => new[] { "Port Elizabeth", "East London", "Mthatha", "Grahamstown", "Uitenhage" },
                "free state" => new[] { "Bloemfontein", "Welkom", "Bethlehem", "Kroonstad", "Sasolburg" },
                "limpopo" => new[] { "Polokwane", "Tzaneen", "Mokopane", "Thohoyandou", "Makhado" },
                "mpumalanga" => new[] { "Nelspruit", "Witbank", "Middelburg", "Secunda", "Ermelo" },
                "northern cape" => new[] { "Kimberley", "Upington", "Springbok", "De Aar" },
                "north west" => new[] { "Rustenburg", "Mahikeng", "Potchefstroom", "Klerksdorp", "Brits" },
                _ => Array.Empty<string>()
            };
        }
    }
}