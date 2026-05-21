using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Payment service interface
    /// Define methods for payment processing
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Create a PayFast payment request for an order
        /// </summary>
        /// <param name="order">The order to create payment for</param>
        /// <returns>PayFast payment request with all required fields</returns>
        PayFastPaymentRequest CreatePaymentRequest(Order order);

        /// <summary>
        /// Generate the payment form HTML
        /// This creates an auto-submitting form that redirects to PayFast
        /// </summary>
        /// <param name="paymentRequest">The payment request data</param>
        /// <returns>HTML form as string</returns>
        string GeneratePaymentForm(PayFastPaymentRequest paymentRequest);

        /// <summary>
        /// Validate PayFast ITN (Instant Transaction Notification)
        /// Ensures the notification is legitimate and from PayFast
        /// </summary>
        /// <param name="notifyData">The notification data from PayFast</param>
        /// <param name="pfParamString">The parameter string for signature validation</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateITNAsync(PayFastNotifyResponse notifyData, string pfParamString);

        /// <summary>
        /// Generate MD5 signature for payment data
        /// Required by PayFast for security
        /// </summary>
        /// <param name="dataString">The data string to sign</param>
        /// <returns>MD5 hash as string</returns>
        string GenerateSignature(string dataString);

        /// <summary>
        /// Convert payment request to parameter string (for signature)
        /// </summary>
        /// <param name="paymentRequest">Payment request data</param>
        /// <returns>URL-encoded parameter string</returns>
        string GetParamString(PayFastPaymentRequest paymentRequest);
    }
}