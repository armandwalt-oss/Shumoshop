using System.Net;
using System.Net.Mail;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// Email service implementation
    /// Configure SMTP settings in appsettings.json
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;
        private readonly string? _smtpHost;
        private readonly int _smtpPort;
        private readonly string? _smtpUsername;
        private readonly string? _smtpPassword;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load SMTP settings from configuration
            _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@shumoshop.co.za";
            _fromName = _configuration["Email:FromName"] ?? "ShumoShop";
            _enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);
            _smtpHost = _configuration["Email:SmtpHost"];
            _smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            _smtpUsername = _configuration["Email:SmtpUsername"];
            _smtpPassword = _configuration["Email:SmtpPassword"];
        }

        public async Task SendOrderConfirmationAsync(Order order, ApplicationUser user)
        {
            var subject = $"Order Confirmation - {order.OrderNumber}";
            var body = GenerateOrderConfirmationHtml(order, user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendShippingNotificationAsync(Order order, ApplicationUser user)
        {
            var subject = $"Your Order Has Been Shipped - {order.OrderNumber}";
            var body = GenerateShippingNotificationHtml(order, user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendDeliveryConfirmationAsync(Order order, ApplicationUser user)
        {
            var subject = $"Order Delivered - {order.OrderNumber}";
            var body = GenerateDeliveryConfirmationHtml(order, user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendOrderCancellationAsync(Order order, ApplicationUser user)
        {
            var subject = $"Order Cancelled - {order.OrderNumber}";
            var body = GenerateOrderCancellationHtml(order, user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(ApplicationUser user, string resetLink)
        {
            var subject = "Password Reset Request";
            var body = GeneratePasswordResetHtml(user, resetLink);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendWelcomeEmailAsync(ApplicationUser user)
        {
            var subject = "Welcome to ShumoShop!";
            var body = GenerateWelcomeEmailHtml(user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendReturnRequestConfirmationAsync(ReturnRequest returnRequest, ApplicationUser user)
        {
            var subject = $"Return Request Received - {returnRequest.ReturnAuthorizationNumber}";
            var body = GenerateReturnRequestHtml(returnRequest, user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendReturnApprovalAsync(ReturnRequest returnRequest, ApplicationUser user)
        {
            var subject = $"Return Approved - {returnRequest.ReturnAuthorizationNumber}";
            var body = GenerateReturnApprovalHtml(returnRequest, user);
            await SendEmailAsync(user.Email!, subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // Check if SMTP is configured
                if (string.IsNullOrEmpty(_smtpHost))
                {
                    _logger.LogWarning("📧 SMTP not configured. Email would be sent to: {Email} | Subject: {Subject}",
                        toEmail, subject);
                    _logger.LogDebug("Email body preview: {Body}", htmlBody.Substring(0, Math.Min(200, htmlBody.Length)));
                    return;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(_fromEmail, _fromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort);
                smtpClient.EnableSsl = _enableSsl;

                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                }

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation("✅ Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
                // Don't throw - email failures shouldn't break the application
            }
        }

        #region HTML Template Generators

        private string GenerateOrderConfirmationHtml(Order order, ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Thank You For Your Order!</h2>
                    <p>Hi {user.Name},</p>
                    <p>We've received your order and it's being processed.</p>
                    
                    <h3>Order Details:</h3>
                    <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                    <p><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</p>
                    <p><strong>Total Amount:</strong> R {order.TotalAmount:N2}</p>
                    
                    <h3>Shipping Address:</h3>
                    <p>
                        {order.ShippingAddress}<br>
                        {order.ShippingCity}, {order.ShippingState}<br>
                        {order.ShippingPostalCode}
                    </p>
                    
                    <p>We'll send you another email when your order ships.</p>
                    
                    <p>Thank you for shopping with ShumoShop!</p>
                </body>
                </html>
            ";
        }

        private string GenerateShippingNotificationHtml(Order order, ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Your Order Has Shipped!</h2>
                    <p>Hi {user.Name},</p>
                    <p>Great news! Your order is on its way.</p>
                    
                    <h3>Shipping Details:</h3>
                    <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                    <p><strong>Tracking Number:</strong> {order.TrackingNumber ?? "Not available yet"}</p>
                    <p><strong>Carrier:</strong> {order.Carrier ?? "N/A"}</p>
                    {(string.IsNullOrEmpty(order.TrackingLink) ? "" : $"<p><a href='{order.TrackingLink}'>Track Your Order</a></p>")}
                    
                    <p><strong>Estimated Delivery:</strong> {order.EstimatedDelivery?.ToString("dd MMM yyyy") ?? "TBD"}</p>
                    
                    <p>Thank you for shopping with ShumoShop!</p>
                </body>
                </html>
            ";
        }

        private string GenerateDeliveryConfirmationHtml(Order order, ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Your Order Has Been Delivered!</h2>
                    <p>Hi {user.Name},</p>
                    <p>Your order has been successfully delivered.</p>
                    
                    <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                    <p><strong>Delivered On:</strong> {order.DeliveredDate:dd MMM yyyy}</p>
                    
                    <p>We hope you enjoy your purchase! If you have any issues, please don't hesitate to contact us.</p>
                    
                    <p>Thank you for shopping with ShumoShop!</p>
                </body>
                </html>
            ";
        }

        private string GenerateOrderCancellationHtml(Order order, ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Order Cancellation Confirmation</h2>
                    <p>Hi {user.Name},</p>
                    <p>Your order has been cancelled as requested.</p>
                    
                    <p><strong>Order Number:</strong> {order.OrderNumber}</p>
                    <p><strong>Cancelled On:</strong> {DateTime.Now:dd MMM yyyy}</p>
                    
                    <p>If you have any questions, please contact our support team.</p>
                    
                    <p>We hope to see you again soon!</p>
                </body>
                </html>
            ";
        }

        private string GeneratePasswordResetHtml(ApplicationUser user, string resetLink)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Password Reset Request</h2>
                    <p>Hi {user.Name},</p>
                    <p>We received a request to reset your password.</p>
                    
                    <p><a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; display: inline-block;'>Reset Password</a></p>
                    
                    <p>If you didn't request this, you can safely ignore this email.</p>
                    <p>This link will expire in 24 hours.</p>
                    
                    <p>Thank you,<br>ShumoShop Team</p>
                </body>
                </html>
            ";
        }

        private string GenerateWelcomeEmailHtml(ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Welcome to ShumoShop!</h2>
                    <p>Hi {user.Name},</p>
                    <p>Thank you for creating an account with us!</p>
                    
                    <p>You can now:</p>
                    <ul>
                        <li>Track your orders</li>
                        <li>Save your favorite items</li>
                        <li>Manage multiple addresses</li>
                        <li>Get exclusive deals and promotions</li>
                    </ul>
                    
                    <p>Start shopping now and enjoy your experience with ShumoShop!</p>
                    
                    <p>Best regards,<br>ShumoShop Team</p>
                </body>
                </html>
            ";
        }

        private string GenerateReturnRequestHtml(ReturnRequest returnRequest, ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Return Request Received</h2>
                    <p>Hi {user.Name},</p>
                    <p>We've received your return request and will process it shortly.</p>
                    
                    <p><strong>Return Authorization Number:</strong> {returnRequest.ReturnAuthorizationNumber}</p>
                    <p><strong>Reason:</strong> {returnRequest.ReturnReason}</p>
                    
                    <p>We'll review your request and get back to you within 2-3 business days.</p>
                    
                    <p>Thank you,<br>ShumoShop Team</p>
                </body>
                </html>
            ";
        }

        private string GenerateReturnApprovalHtml(ReturnRequest returnRequest, ApplicationUser user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Return Request Approved</h2>
                    <p>Hi {user.Name},</p>
                    <p>Good news! Your return request has been approved.</p>
                    
                    <p><strong>Return Authorization Number:</strong> {returnRequest.ReturnAuthorizationNumber}</p>
                    <p><strong>Refund Amount:</strong> R {returnRequest.RefundAmount:N2}</p>
                    
                    <p>Please ship the item back to us within 7 days. Once we receive it, we'll process your refund.</p>
                    
                    <p>Thank you,<br>ShumoShop Team</p>
                </body>
                </html>
            ";
        }

        #endregion
    }
}