using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// A market ShumoShop ships to. Drives geo-pricing (per-country product
    /// prices, currency display) and shipping (which carriers are offered).
    /// </summary>
    public class Country
    {
        [Key]
        [StringLength(2)]
        [Display(Name = "ISO Code")]
        public string Code { get; set; } = string.Empty;   // ISO-3166 alpha-2, e.g. "ZA", "US", "GB"

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;   // e.g. "South Africa"

        [Required]
        [StringLength(3)]
        public string CurrencyCode { get; set; } = "ZAR";  // ISO-4217, e.g. "ZAR", "USD", "GBP"

        [Required]
        [StringLength(5)]
        public string CurrencySymbol { get; set; } = "R";  // "R", "$", "£", "€"

        [StringLength(8)]
        [Display(Name = "Flag emoji")]
        public string? FlagEmoji { get; set; }             // "🇿🇦", "🇺🇸" — optional

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 100;
    }
}
