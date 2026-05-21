using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Net;

namespace WebApplication1.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Existing fields
        public string Name { get; set; } = string.Empty;
        
        // NEW: Surname field
        public string Surname { get; set; } = string.Empty;
        
        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        // NEW fields for My Account feature
        public string? ProfilePictureUrl { get; set; }
        public DateTime DateJoined { get; set; } = DateTime.Now;
        public bool EmailNotifications { get; set; } = true;
        public bool SMSNotifications { get; set; } = false;
        public bool PromotionalEmails { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }

        // Soft-delete: customer asked to delete their account. We keep the
        // row (with anonymised PII) so historical orders still link to a user
        // for audit / accounting / tax retention.
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeactivatedDate { get; set; }

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<Wishlist> WishlistItems { get; set; } = new List<Wishlist>();
        
        // Helper property to get full name
        public string FullName => $"{Name} {Surname}".Trim();
    }
}
