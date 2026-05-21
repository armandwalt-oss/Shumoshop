using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class RegisterUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        // Change label to Name (not "Full Name" anymore)
        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        // ✅ ADD THIS
        [Required]
        [StringLength(100)]
        [Display(Name = "Surname")]
        public string Surname { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "State")]
        public string State { get; set; }

        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }
    }
}
