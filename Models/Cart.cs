using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }

        // For logged-in users
        public string? UserId { get; set; }

        // For guest users (session-based)
        [StringLength(100)]
        public string? SessionId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Calculated properties
        [NotMapped]
        public decimal SubTotal => CartItems.Sum(item => item.TotalPrice);

        [NotMapped]
        public int TotalItems => CartItems.Sum(item => item.Quantity);
    }
}