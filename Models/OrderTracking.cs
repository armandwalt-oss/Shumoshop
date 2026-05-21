using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class OrderTracking
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Estimated Delivery")]
        public DateTime? EstimatedDelivery { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }
}