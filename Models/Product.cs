using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(50)]
        public string SKU { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public bool InStock { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public bool IsNewArrival { get; set; } = false;

        public bool IsSpecial { get; set; } = false;

        public int StockQuantity { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // NEW: Optional SubCategory relationship
        public int? SubCategoryId { get; set; }

        [ForeignKey("SubCategoryId")]
        public virtual SubCategory? SubCategory { get; set; }
    }
}
