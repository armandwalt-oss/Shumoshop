using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        // Order relationship
        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        // Product relationship
        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // Product details at time of order (in case product changes later)
        [Required]
        [StringLength(200)]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Product SKU")]
        public string ProductSKU { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        // Quantity and pricing
        [Required]
        [Range(1, 1000)]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Price")]
        public decimal TotalPrice { get; set; } // Quantity * UnitPrice

        // Additional info
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}