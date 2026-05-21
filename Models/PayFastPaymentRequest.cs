using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    /// <summary>
    /// PayFast payment request model
    /// Represents all data sent to PayFast for payment processing
    /// </summary>
    public class PayFastPaymentRequest
    {
        // ============================================
        // MERCHANT DETAILS (from configuration)
        // ============================================

        [Required]
        public string merchant_id { get; set; } = string.Empty;

        [Required]
        public string merchant_key { get; set; } = string.Empty;

        // ============================================
        // TRANSACTION DETAILS
        // ============================================

        /// <summary>
        /// Return URL - where customer goes after successful payment
        /// </summary>
        [Required]
        public string return_url { get; set; } = string.Empty;

        /// <summary>
        /// Cancel URL - where customer goes if they cancel
        /// </summary>
        [Required]
        public string cancel_url { get; set; } = string.Empty;

        /// <summary>
        /// Notify URL - where PayFast sends ITN (Instant Transaction Notification)
        /// </summary>
        [Required]
        public string notify_url { get; set; } = string.Empty;

        // ============================================
        // BUYER DETAILS
        // ============================================

        [Required]
        [StringLength(100)]
        public string name_first { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string name_last { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string email_address { get; set; } = string.Empty;

        [Phone]
        [StringLength(15)]
        public string? cell_number { get; set; }

        // ============================================
        // TRANSACTION DETAILS
        // ============================================

        /// <summary>
        /// Your unique order/invoice number (e.g., ORD-20260205-000123)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string m_payment_id { get; set; } = string.Empty;

        /// <summary>
        /// Total amount in ZAR (e.g., 1250.50)
        /// </summary>
        [Required]
        public decimal amount { get; set; }

        /// <summary>
        /// Description of items (e.g., "Order #ORD-20260205-000123")
        /// </summary>
        [Required]
        [StringLength(255)]
        public string item_name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description (optional)
        /// </summary>
        [StringLength(255)]
        public string? item_description { get; set; }

        // ============================================
        // OPTIONAL CUSTOM FIELDS (for your reference)
        // ============================================

        /// <summary>
        /// Custom string 1 - we'll use for Order ID
        /// </summary>
        public string? custom_str1 { get; set; }

        /// <summary>
        /// Custom string 2 - we'll use for User ID
        /// </summary>
        public string? custom_str2 { get; set; }

        /// <summary>
        /// Custom int 1 - additional reference
        /// </summary>
        public int? custom_int1 { get; set; }

        // ============================================
        // SECURITY SIGNATURE (calculated before sending)
        // ============================================

        /// <summary>
        /// MD5 signature of all fields - calculated by PayFastService
        /// Ensures data integrity
        /// </summary>
        public string? signature { get; set; }
    }
}