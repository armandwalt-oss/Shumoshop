using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(200)]
        public string IconName { get; set; }

        // This is calculated from products, not stored in DB
        [NotMapped]
        public int ProductCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Display order for navbar sorting
        public int DisplayOrder { get; set; } = 0;

        // Navigation property to SubCategories
        public virtual ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
