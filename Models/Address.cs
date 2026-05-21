using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }

        // User relationship
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Address type
        [Required]
        [StringLength(50)]
        [Display(Name = "Address Type")]
        public string AddressType { get; set; } = "Shipping"; // Shipping, Billing, Both

        // Address nickname (optional - e.g., "Home", "Work", "Mom's House")
        [StringLength(50)]
        [Display(Name = "Address Label")]
        public string? Label { get; set; }

        // Contact name for this address
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // Address details
        [Required]
        [StringLength(200)]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Apartment/Unit")]
        public string? ApartmentUnit { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "State/Province")]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string Country { get; set; } = "South Africa";

        // Contact information
        [Required]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Flags
        [Display(Name = "Default Shipping Address")]
        public bool IsDefaultShipping { get; set; } = false;

        [Display(Name = "Default Billing Address")]
        public bool IsDefaultBilling { get; set; } = false;

        // Additional instructions (e.g., "Ring doorbell twice", "Leave at gate")
        [StringLength(500)]
        [Display(Name = "Delivery Instructions")]
        public string? DeliveryInstructions { get; set; }
    }
}