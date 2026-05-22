using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// One row per product detail page hit. Used to compute "top viewed"
    /// and trending lists in the admin. Privacy-friendly: no IP, no UA,
    /// just the timestamp and (optionally) the signed-in user.
    /// </summary>
    public class ProductView
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Null = anonymous visitor.</summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        /// <summary>Active country code at view time (for market-segmented analytics).</summary>
        [StringLength(2)]
        public string? CountryCode { get; set; }
    }
}
