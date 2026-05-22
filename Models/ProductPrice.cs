using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// A per-country override for a product's price. Falls back to
    /// <c>Product.Price</c> (interpreted as ZAR) when no row exists for the
    /// visitor's active country.
    /// </summary>
    public class ProductPrice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        /// <summary>ISO-3166 alpha-2, matches Country.Code.</summary>
        [Required]
        [StringLength(2)]
        public string CountryCode { get; set; } = string.Empty;

        [ForeignKey("CountryCode")]
        public virtual Country Country { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UpdatedByUserId { get; set; }
    }
}
