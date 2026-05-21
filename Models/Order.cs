using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Order Number")]
        public string OrderNumber { get; set; } = string.Empty;

        // User relationship
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        // Order dates
        [Required]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Shipped Date")]
        public DateTime? ShippedDate { get; set; }

        [Display(Name = "Delivered Date")]
        public DateTime? DeliveredDate { get; set; }

        [Display(Name = "Estimated Delivery")]
        public DateTime? EstimatedDelivery { get; set; }

        // Order status
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        // Pricing
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Subtotal")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Shipping Cost")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tax")]
        public decimal Tax { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        // Shipping Information
        [Required]
        [StringLength(200)]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Shipping City")]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Shipping State")]
        public string ShippingState { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Shipping Postal Code")]
        public string ShippingPostalCode { get; set; } = string.Empty;

        // Billing Information (can be same as shipping)
        [StringLength(200)]
        [Display(Name = "Billing Address")]
        public string? BillingAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "Billing City")]
        public string? BillingCity { get; set; }

        [StringLength(100)]
        [Display(Name = "Billing State")]
        public string? BillingState { get; set; }

        [StringLength(20)]
        [Display(Name = "Billing Postal Code")]
        public string? BillingPostalCode { get; set; }

        // Contact information
        [Required]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Customer Surname")]
        public string CustomerSurname { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Payment information
        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; } // Credit Card, PayPal, Cash on Delivery, etc.

        [StringLength(50)]
        [Display(Name = "Payment Status")]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed, Refunded

        [StringLength(100)]
        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }

        // Tracking
        [StringLength(100)]
        [Display(Name = "Tracking Number")]
        public string? TrackingNumber { get; set; }

        [StringLength(50)]
        [Display(Name = "Carrier")]
        public string? Carrier { get; set; } // DHL, FedEx, UPS, etc.

        [StringLength(500)]
        [Display(Name = "Tracking Link")]
        public string? TrackingLink { get; set; }

        // Additional notes
        [StringLength(1000)]
        [Display(Name = "Order Notes")]
        public string? Notes { get; set; }

        [StringLength(1000)]
        [Display(Name = "Customer Comments")]
        public string? CustomerComments { get; set; }

        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}