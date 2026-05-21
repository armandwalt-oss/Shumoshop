using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ContactUs
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter your name")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Please enter your email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter a subject")]
        [StringLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Please enter your message")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string Message { get; set; }

        [Display(Name = "Submitted Date")]
        public DateTime SubmittedDate { get; set; }
    }
}