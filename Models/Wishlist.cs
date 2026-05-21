using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Wishlist
    {
        [Key]
        public int Id { get; set; }

        // For logged-in users
        public string? UserId { get; set; }

        // For guest users (session)
        public string? SessionId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    }

    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WishlistId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("WishlistId")]
        public virtual Wishlist Wishlist { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}