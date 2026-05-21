using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;
using System.Security.Claims;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Payment Controller - Handles PayFast payment processing
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            IPaymentService paymentService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        // ============================================
        // INITIATE PAYMENT - Redirect to PayFast
        // ============================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Process(int orderId)
        {
            _logger.LogInformation("Processing payment for order ID: {OrderId}", orderId);

            // Get the order
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", orderId);
                return NotFound("Order not found");
            }

            // Verify this order belongs to the current user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.UserId != userId)
            {
                _logger.LogWarning("Unauthorized payment attempt for order: {OrderId} by user: {UserId}", orderId, userId);
                return Forbid();
            }

            // Check if order is already paid
            if (order.PaymentStatus == "Paid")
            {
                _logger.LogWarning("Order already paid: {OrderNumber}", order.OrderNumber);
                return RedirectToAction("OrderConfirmation", "Order", new { orderNumber = order.OrderNumber });
            }

            try
            {
                // Create PayFast payment request
                var paymentRequest = _paymentService.CreatePaymentRequest(order);

                // Generate the payment form HTML
                string formHtml = _paymentService.GeneratePaymentForm(paymentRequest);

                // Update order to show payment is being processed
                order.PaymentStatus = "Processing";
                order.Notes = $"Payment initiated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment form generated for order: {OrderNumber}", order.OrderNumber);

                // Return the HTML form (it will auto-submit to PayFast)
                return Content(formHtml, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order: {OrderNumber}", order.OrderNumber);
                TempData["ErrorMessage"] = "Failed to process payment. Please try again.";
                return RedirectToAction("Checkout", "Order");
            }
        }

        // ============================================
        // RETURN URL - Customer returns from PayFast
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Return(string m_payment_id)
        {
            _logger.LogInformation("Customer returned from PayFast for payment: {PaymentId}", m_payment_id);

            if (string.IsNullOrEmpty(m_payment_id))
            {
                _logger.LogWarning("Return URL called with no payment ID");
                return RedirectToAction("Index", "Home");
            }

            // Find the order by order number
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderNumber == m_payment_id);

            if (order == null)
            {
                _logger.LogWarning("Order not found for payment ID: {PaymentId}", m_payment_id);
                TempData["ErrorMessage"] = "Order not found";
                return RedirectToAction("Index", "Home");
            }

            // Check payment status
            if (order.PaymentStatus == "Paid")
            {
                // Payment successful!
                ViewBag.Order = order;
                ViewBag.Message = "Payment successful! Your order is being processed.";
                return View("Success");
            }
            else if (order.PaymentStatus == "Failed")
            {
                // Payment failed
                ViewBag.Order = order;
                ViewBag.Message = "Payment was not successful. Please try again.";
                return View("Failed");
            }
            else
            {
                // Payment still pending (ITN not received yet)
                ViewBag.Order = order;
                ViewBag.Message = "Your payment is being processed. Please wait for confirmation.";
                return View("Pending");
            }
        }

        // ============================================
        // CANCEL URL - Customer cancelled payment
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Cancel(string m_payment_id)
        {
            _logger.LogInformation("Customer cancelled payment for: {PaymentId}", m_payment_id);

            if (!string.IsNullOrEmpty(m_payment_id))
            {
                // Find the order
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == m_payment_id);

                if (order != null)
                {
                    // Update payment status
                    order.PaymentStatus = "Cancelled";
                    order.Notes = $"Payment cancelled by customer at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Payment cancelled for order: {OrderNumber}", order.OrderNumber);

                    ViewBag.Order = order;
                    return View();
                }
            }

            TempData["ErrorMessage"] = "Payment was cancelled";
            return RedirectToAction("Index", "Cart");
        }

        // ============================================
        // NOTIFY URL - PayFast ITN (Instant Transaction Notification)
        // PayFast POSTs here server-to-server after a payment completes.
        // Must return HTTP 200 within ~10s or PayFast will keep retrying.
        // PayFast posts from their servers, not the browser, so anti-forgery
        // tokens do not (and cannot) apply — exempt explicitly.
        // ============================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Notify()
        {
            _logger.LogInformation("📧 Received ITN from PayFast");

            try
            {
                // Read the POST data from PayFast
                var formData = new Dictionary<string, string>();
                foreach (var key in Request.Form.Keys)
                {
                    formData[key] = Request.Form[key].ToString();
                }

                // Log the received data (for debugging)
                _logger.LogInformation("ITN Data: {Data}", string.Join(", ", formData.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                // Parse the notification data
                var notifyData = new PayFastNotifyResponse
                {
                    m_payment_id = formData.ContainsKey("m_payment_id") ? formData["m_payment_id"] : null,
                    pf_payment_id = formData.ContainsKey("pf_payment_id") ? formData["pf_payment_id"] : null,
                    payment_status = formData.ContainsKey("payment_status") ? formData["payment_status"] : null,
                    item_name = formData.ContainsKey("item_name") ? formData["item_name"] : null,
                    item_description = formData.ContainsKey("item_description") ? formData["item_description"] : null,
                    amount_gross = formData.ContainsKey("amount_gross") ? decimal.Parse(formData["amount_gross"]) : 0,
                    amount_fee = formData.ContainsKey("amount_fee") ? decimal.Parse(formData["amount_fee"]) : 0,
                    amount_net = formData.ContainsKey("amount_net") ? decimal.Parse(formData["amount_net"]) : 0,
                    custom_str1 = formData.ContainsKey("custom_str1") ? formData["custom_str1"] : null,
                    custom_str2 = formData.ContainsKey("custom_str2") ? formData["custom_str2"] : null,
                    custom_int1 = formData.ContainsKey("custom_int1") ? int.Parse(formData["custom_int1"]) : null,
                    name_first = formData.ContainsKey("name_first") ? formData["name_first"] : null,
                    name_last = formData.ContainsKey("name_last") ? formData["name_last"] : null,
                    email_address = formData.ContainsKey("email_address") ? formData["email_address"] : null,
                    signature = formData.ContainsKey("signature") ? formData["signature"] : null
                };

                // Build parameter string for validation (exclude signature)
                var paramList = formData.Where(kvp => kvp.Key != "signature")
                                        .OrderBy(kvp => kvp.Key)
                                        .Select(kvp => $"{kvp.Key}={System.Web.HttpUtility.UrlEncode(kvp.Value)}");
                string pfParamString = string.Join("&", paramList);

                // Validate the ITN
                bool isValid = await _paymentService.ValidateITNAsync(notifyData, pfParamString);

                if (!isValid)
                {
                    _logger.LogWarning("❌ Invalid ITN received for payment: {PaymentId}", notifyData.m_payment_id);
                    return StatusCode(400, "Invalid ITN");
                }

                _logger.LogInformation("✅ ITN validation passed for payment: {PaymentId}", notifyData.m_payment_id);

                // Find the order
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderNumber == notifyData.m_payment_id);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for ITN payment: {PaymentId}", notifyData.m_payment_id);
                    return NotFound("Order not found");
                }

                // Update order based on payment status
                if (notifyData.payment_status == "COMPLETE")
                {
                    _logger.LogInformation("💰 Payment COMPLETE for order: {OrderNumber}", order.OrderNumber);

                    // Update order status
                    order.Status = "Processing";
                    order.PaymentStatus = "Paid";
                    order.PaymentMethod = "PayFast";
                    order.TransactionId = notifyData.pf_payment_id;
                    order.Notes = $"Payment completed via PayFast at {DateTime.Now:yyyy-MM-dd HH:mm:ss}. Transaction ID: {notifyData.pf_payment_id}";

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ Order updated successfully for: {OrderNumber}", order.OrderNumber);

                    // Send order confirmation email
                    try
                    {
                        var user = order.User;
                        if (user != null)
                        {
                            await _emailService.SendOrderConfirmationAsync(order, user);
                            _logger.LogInformation("📧 Order confirmation email sent for: {OrderNumber}", order.OrderNumber);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send order confirmation email for: {OrderNumber}", order.OrderNumber);
                        // Don't fail the ITN if email fails
                    }

                    // Notify the shop admin that a new paid order has landed.
                    try
                    {
                        var adminEmail = _configuration["Email:AdminEmail"];
                        if (!string.IsNullOrWhiteSpace(adminEmail))
                        {
                            var subject = $"New paid order — {order.OrderNumber} (R {order.TotalAmount:N2})";
                            var body = $@"
                                <html><body style='font-family:Arial,sans-serif;'>
                                <h2>New paid order</h2>
                                <p><strong>Order:</strong> {order.OrderNumber}</p>
                                <p><strong>Customer:</strong> {System.Net.WebUtility.HtmlEncode(order.CustomerName)} {System.Net.WebUtility.HtmlEncode(order.CustomerSurname)} &lt;{System.Net.WebUtility.HtmlEncode(order.Email)}&gt;</p>
                                <p><strong>Total:</strong> R {order.TotalAmount:N2}</p>
                                <p><strong>Shipping to:</strong> {System.Net.WebUtility.HtmlEncode(order.ShippingAddress)}, {System.Net.WebUtility.HtmlEncode(order.ShippingCity)}, {System.Net.WebUtility.HtmlEncode(order.ShippingState)}, {System.Net.WebUtility.HtmlEncode(order.ShippingPostalCode)}</p>
                                <p><strong>PayFast transaction:</strong> {System.Net.WebUtility.HtmlEncode(order.TransactionId ?? "")}</p>
                                <p style='margin-top:24px;'><a href='/Admin/OrderDetails/{order.Id}'>Open order in admin →</a></p>
                                </body></html>";

                            await _emailService.SendEmailAsync(adminEmail, subject, body);
                            _logger.LogInformation("📧 Admin new-order notification sent for: {OrderNumber}", order.OrderNumber);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send admin new-order email for: {OrderNumber}", order.OrderNumber);
                    }

                    // Return success to PayFast
                    return Ok("ITN processed successfully");
                }
                else if (notifyData.payment_status == "FAILED")
                {
                    _logger.LogWarning("❌ Payment FAILED for order: {OrderNumber}", order.OrderNumber);

                    order.PaymentStatus = "Failed";
                    order.Notes = $"Payment failed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    await _context.SaveChangesAsync();

                    return Ok("ITN processed - payment failed");
                }
                else if (notifyData.payment_status == "CANCELLED")
                {
                    _logger.LogInformation("🚫 Payment CANCELLED for order: {OrderNumber}", order.OrderNumber);

                    order.PaymentStatus = "Cancelled";
                    order.Notes = $"Payment cancelled at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    await _context.SaveChangesAsync();

                    return Ok("ITN processed - payment cancelled");
                }
                else
                {
                    _logger.LogWarning("⚠️ Unknown payment status: {Status} for order: {OrderNumber}",
                        notifyData.payment_status, order.OrderNumber);
                    return Ok("ITN received");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing ITN");
                return StatusCode(500, "Error processing ITN");
            }
        }
    }
}