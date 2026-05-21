using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // Calculated property
        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}