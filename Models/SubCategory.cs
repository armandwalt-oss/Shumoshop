using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class SubCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(200)]
        public string IconName { get; set; }

        // Foreign key to Category
        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        // Display order for sorting
        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation property to Products
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
