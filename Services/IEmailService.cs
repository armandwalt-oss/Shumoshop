using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Interface for email service
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends order confirmation email to customer
        /// </summary>
        Task SendOrderConfirmationAsync(Order order, ApplicationUser user);

        /// <summary>
        /// Sends shipping notification when order is shipped
        /// </summary>
        Task SendShippingNotificationAsync(Order order, ApplicationUser user);

        /// <summary>
        /// Sends delivery confirmation when order is delivered
        /// </summary>
        Task SendDeliveryConfirmationAsync(Order order, ApplicationUser user);

        /// <summary>
        /// Sends order cancellation notification
        /// </summary>
        Task SendOrderCancellationAsync(Order order, ApplicationUser user);

        /// <summary>
        /// Sends password reset email
        /// </summary>
        Task SendPasswordResetEmailAsync(ApplicationUser user, string resetLink);

        /// <summary>
        /// Sends welcome email to new users
        /// </summary>
        Task SendWelcomeEmailAsync(ApplicationUser user);

        /// <summary>
        /// Sends return request confirmation
        /// </summary>
        Task SendReturnRequestConfirmationAsync(ReturnRequest returnRequest, ApplicationUser user);

        /// <summary>
        /// Sends return approval notification
        /// </summary>
        Task SendReturnApprovalAsync(ReturnRequest returnRequest, ApplicationUser user);

        /// <summary>
        /// Sends generic notification email
        /// </summary>
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
}