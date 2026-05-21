namespace WebApplication1.Models
{
    /// <summary>
    /// PayFast ITN (Instant Transaction Notification) Response
    /// This is what PayFast sends back to your NotifyUrl when payment is complete
    /// </summary>
    public class PayFastNotifyResponse
    {
        // ============================================
        // MERCHANT DETAILS
        // ============================================

        public string? m_payment_id { get; set; }  // Your order number
        public string? pf_payment_id { get; set; }  // PayFast transaction ID

        // ============================================
        // PAYMENT STATUS
        // ============================================

        /// <summary>
        /// Payment status: COMPLETE, FAILED, PENDING, CANCELLED
        /// </summary>
        public string? payment_status { get; set; }

        // ============================================
        // ITEM DETAILS
        // ============================================

        public string? item_name { get; set; }
        public string? item_description { get; set; }

        // ============================================
        // AMOUNTS
        // ============================================

        public decimal amount_gross { get; set; }  // Total amount paid
        public decimal amount_fee { get; set; }    // PayFast fee
        public decimal amount_net { get; set; }    // Amount you receive

        // ============================================
        // CUSTOM FIELDS (what you sent)
        // ============================================

        public string? custom_str1 { get; set; }  // Order ID
        public string? custom_str2 { get; set; }  // User ID
        public int? custom_int1 { get; set; }

        // ============================================
        // BUYER DETAILS
        // ============================================

        public string? name_first { get; set; }
        public string? name_last { get; set; }
        public string? email_address { get; set; }

        // ============================================
        // BILLING DETAILS (if provided by customer)
        // ============================================

        public string? billing_date { get; set; }

        // ============================================
        // SECURITY
        // ============================================

        /// <summary>
        /// PayFast's signature - we validate this to ensure it's really from PayFast
        /// </summary>
        public string? signature { get; set; }
    }
}