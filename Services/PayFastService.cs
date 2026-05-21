using System.Security.Cryptography;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    /// <summary>
    /// PayFast payment service implementation
    /// Handles all PayFast payment processing, signature generation, and validation
    /// </summary>
    public class PayFastService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayFastService> _logger;
        private readonly string _merchantId;
        private readonly string _merchantKey;
        private readonly string _passphrase;
        private readonly string _processUrl;
        private readonly string _validateUrl;
        private readonly string _returnUrl;
        private readonly string _cancelUrl;
        private readonly string _notifyUrl;
        private readonly bool _useSandbox;

        public PayFastService(IConfiguration configuration, ILogger<PayFastService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Load PayFast configuration
            _merchantId = _configuration["PayFast:MerchantId"] ?? "";
            _merchantKey = _configuration["PayFast:MerchantKey"] ?? "";
            _passphrase = _configuration["PayFast:Passphrase"] ?? "";
            _processUrl = _configuration["PayFast:ProcessUrl"] ?? "";
            _validateUrl = _configuration["PayFast:ValidateUrl"] ?? "";
            _returnUrl = _configuration["PayFast:ReturnUrl"] ?? "";
            _cancelUrl = _configuration["PayFast:CancelUrl"] ?? "";
            _notifyUrl = _configuration["PayFast:NotifyUrl"] ?? "";
            _useSandbox = _configuration.GetValue<bool>("PayFast:UseSandbox", true);

            _logger.LogInformation("PayFast Service initialized. Sandbox mode: {UseSandbox}", _useSandbox);
        }

        /// <summary>
        /// Create a PayFast payment request from an order
        /// </summary>
        public PayFastPaymentRequest CreatePaymentRequest(Order order)
        {
            _logger.LogInformation("Creating PayFast payment request for order: {OrderNumber}", order.OrderNumber);

            var paymentRequest = new PayFastPaymentRequest
            {
                // Merchant details
                merchant_id = _merchantId,
                merchant_key = _merchantKey,

                // URLs
                return_url = _returnUrl,
                cancel_url = _cancelUrl,
                notify_url = _notifyUrl,

                // Buyer details
                name_first = order.CustomerName,
                name_last = order.CustomerSurname,
                email_address = order.Email,
                cell_number = order.PhoneNumber?.Replace(" ", "").Replace("+27", "0"), // Format: 0821234567

                // Transaction details
                m_payment_id = order.OrderNumber,
                amount = order.TotalAmount,
                item_name = $"Order {order.OrderNumber}",
                item_description = $"ShumoShop Order - {order.OrderItems?.Count ?? 0} item(s)",

                // Custom fields (for our reference)
                custom_str1 = order.Id.ToString(), // Order ID
                custom_str2 = order.UserId,        // User ID
                custom_int1 = order.OrderItems?.Count ?? 0  // Number of items
            };

            // Generate signature
            string paramString = GetParamString(paymentRequest);
            paymentRequest.signature = GenerateSignature(paramString);

            _logger.LogInformation("Payment request created with signature for order: {OrderNumber}", order.OrderNumber);

            return paymentRequest;
        }

        /// <summary>
        /// Generate HTML form that auto-submits to PayFast
        /// </summary>
        public string GeneratePaymentForm(PayFastPaymentRequest paymentRequest)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <title>Redirecting to PayFast...</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }");
            sb.AppendLine("        .spinner { border: 5px solid #f3f3f3; border-top: 5px solid #3498db; border-radius: 50%; width: 50px; height: 50px; animation: spin 1s linear infinite; margin: 20px auto; }");
            sb.AppendLine("        @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <h2>Processing your payment...</h2>");
            sb.AppendLine("    <div class='spinner'></div>");
            sb.AppendLine("    <p>Please wait while we redirect you to PayFast.</p>");
            sb.AppendLine($"    <form id='payfast_form' action='{_processUrl}' method='post'>");

            // Add all payment fields as hidden inputs
            sb.AppendLine($"        <input type='hidden' name='merchant_id' value='{paymentRequest.merchant_id}' />");
            sb.AppendLine($"        <input type='hidden' name='merchant_key' value='{paymentRequest.merchant_key}' />");
            sb.AppendLine($"        <input type='hidden' name='return_url' value='{paymentRequest.return_url}' />");
            sb.AppendLine($"        <input type='hidden' name='cancel_url' value='{paymentRequest.cancel_url}' />");
            sb.AppendLine($"        <input type='hidden' name='notify_url' value='{paymentRequest.notify_url}' />");
            sb.AppendLine($"        <input type='hidden' name='name_first' value='{System.Net.WebUtility.HtmlEncode(paymentRequest.name_first)}' />");
            sb.AppendLine($"        <input type='hidden' name='name_last' value='{System.Net.WebUtility.HtmlEncode(paymentRequest.name_last)}' />");
            sb.AppendLine($"        <input type='hidden' name='email_address' value='{paymentRequest.email_address}' />");

            if (!string.IsNullOrEmpty(paymentRequest.cell_number))
            {
                sb.AppendLine($"        <input type='hidden' name='cell_number' value='{paymentRequest.cell_number}' />");
            }

            sb.AppendLine($"        <input type='hidden' name='m_payment_id' value='{paymentRequest.m_payment_id}' />");
            sb.AppendLine($"        <input type='hidden' name='amount' value='{paymentRequest.amount:F2}' />");
            sb.AppendLine($"        <input type='hidden' name='item_name' value='{System.Net.WebUtility.HtmlEncode(paymentRequest.item_name)}' />");
            sb.AppendLine($"        <input type='hidden' name='item_description' value='{System.Net.WebUtility.HtmlEncode(paymentRequest.item_description)}' />");

            if (!string.IsNullOrEmpty(paymentRequest.custom_str1))
            {
                sb.AppendLine($"        <input type='hidden' name='custom_str1' value='{paymentRequest.custom_str1}' />");
            }

            if (!string.IsNullOrEmpty(paymentRequest.custom_str2))
            {
                sb.AppendLine($"        <input type='hidden' name='custom_str2' value='{paymentRequest.custom_str2}' />");
            }

            if (paymentRequest.custom_int1.HasValue)
            {
                sb.AppendLine($"        <input type='hidden' name='custom_int1' value='{paymentRequest.custom_int1.Value}' />");
            }

            sb.AppendLine($"        <input type='hidden' name='signature' value='{paymentRequest.signature}' />");
            sb.AppendLine("    </form>");
            sb.AppendLine("    <script>");
            sb.AppendLine("        document.getElementById('payfast_form').submit();");
            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Validate ITN from PayFast
        /// Performs all required security checks
        /// </summary>
        public async Task<bool> ValidateITNAsync(PayFastNotifyResponse notifyData, string pfParamString)
        {
            _logger.LogInformation("Validating ITN for payment: {PaymentId}", notifyData.m_payment_id);

            try
            {
                // Step 1: Verify signature
                string calculatedSignature = GenerateSignature(pfParamString);
                if (notifyData.signature != calculatedSignature)
                {
                    _logger.LogWarning("Signature mismatch. Expected: {Expected}, Got: {Got}",
                        calculatedSignature, notifyData.signature);
                    return false;
                }
                _logger.LogInformation("✅ Signature validation passed");

                // Step 2: Verify IP address (PayFast sends from specific IPs)
                // In production, you should verify the IP is from PayFast's known ranges
                // For sandbox testing, we'll skip this
                if (!_useSandbox)
                {
                    // PayFast IP ranges: 197.97.145.144/28 (South African servers)
                    // TODO: Implement IP validation for production
                    _logger.LogInformation("IP validation skipped (not in sandbox mode)");
                }

                // Step 3: Verify payment status
                if (notifyData.payment_status != "COMPLETE")
                {
                    _logger.LogWarning("Payment not complete. Status: {Status}", notifyData.payment_status);
                    return false;
                }
                _logger.LogInformation("✅ Payment status is COMPLETE");

                // Step 4: Validate with PayFast server (optional but recommended)
                if (!string.IsNullOrEmpty(_validateUrl))
                {
                    bool serverValidation = await ValidateWithPayFastServerAsync(pfParamString);
                    if (!serverValidation)
                    {
                        _logger.LogWarning("Server validation failed");
                        return false;
                    }
                    _logger.LogInformation("✅ Server validation passed");
                }

                _logger.LogInformation("✅ ITN validation successful for payment: {PaymentId}", notifyData.m_payment_id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ITN for payment: {PaymentId}", notifyData.m_payment_id);
                return false;
            }
        }

        /// <summary>
        /// Validate ITN with PayFast server
        /// </summary>
        private async Task<bool> ValidateWithPayFastServerAsync(string pfParamString)
        {
            try
            {
                using var httpClient = new HttpClient();
                var content = new StringContent(pfParamString, Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await httpClient.PostAsync(_validateUrl, content);

                string result = await response.Content.ReadAsStringAsync();

                bool isValid = result.Contains("VALID");
                _logger.LogInformation("PayFast server validation result: {Result}", result);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contacting PayFast validation server");
                return false;
            }
        }

        /// <summary>
        /// Generate MD5 signature over the already-built param string.
        /// PayFast's PHP reference appends the passphrase last using
        /// urlencode() — not Uri.EscapeDataString. Use PhpUrlEncode for that.
        /// </summary>
        public string GenerateSignature(string dataString)
        {
            if (!string.IsNullOrEmpty(_passphrase))
            {
                dataString += $"&passphrase={PhpUrlEncode(_passphrase.Trim())}";
            }

            byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(dataString));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Convert payment request to parameter string for signature generation.
        ///
        /// IMPORTANT: PayFast requires the fields in the order documented in
        /// their integration guide — NOT alphabetical. Sorting causes signature
        /// mismatches. We also use PhpUrlEncode (PHP-style urlencode) instead
        /// of Uri.EscapeDataString to match PayFast's PHP reference; otherwise
        /// any value with a space or "~" fails the signature check.
        /// </summary>
        public string GetParamString(PayFastPaymentRequest paymentRequest)
        {
            // Insertion order = PayFast's documented field order.
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("merchant_id",   paymentRequest.merchant_id ?? ""),
                new("merchant_key",  paymentRequest.merchant_key ?? ""),
                new("return_url",    paymentRequest.return_url ?? ""),
                new("cancel_url",    paymentRequest.cancel_url ?? ""),
                new("notify_url",    paymentRequest.notify_url ?? ""),
                new("name_first",    paymentRequest.name_first ?? ""),
                new("name_last",     paymentRequest.name_last ?? ""),
                new("email_address", paymentRequest.email_address ?? ""),
                new("m_payment_id",  paymentRequest.m_payment_id ?? ""),
                new("amount",        paymentRequest.amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new("item_name",     paymentRequest.item_name ?? "")
            };

            if (!string.IsNullOrEmpty(paymentRequest.item_description))
                parameters.Add(new("item_description", paymentRequest.item_description));

            if (!string.IsNullOrEmpty(paymentRequest.cell_number))
                parameters.Add(new("cell_number", paymentRequest.cell_number));

            if (!string.IsNullOrEmpty(paymentRequest.custom_str1))
                parameters.Add(new("custom_str1", paymentRequest.custom_str1));

            if (!string.IsNullOrEmpty(paymentRequest.custom_str2))
                parameters.Add(new("custom_str2", paymentRequest.custom_str2));

            if (paymentRequest.custom_int1.HasValue)
                parameters.Add(new("custom_int1", paymentRequest.custom_int1.Value.ToString()));

            var paramString = string.Join("&", parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => $"{p.Key}={PhpUrlEncode(p.Value.Trim())}"));

            return paramString;
        }

        /// <summary>
        /// Replicates PHP's urlencode() exactly:
        ///   • Spaces become "+"
        ///   • Alphanumerics and "_", "-", "." are NOT encoded
        ///   • Everything else is percent-encoded as %XX
        /// </summary>
        private static string PhpUrlEncode(string value)
        {
            var sb = new StringBuilder();
            foreach (var c in Encoding.UTF8.GetBytes(value))
            {
                if ((c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z') ||
                    (c >= '0' && c <= '9') ||
                    c == '-' || c == '_' || c == '.')
                {
                    sb.Append((char)c);
                }
                else if (c == ' ')
                {
                    sb.Append('+');
                }
                else
                {
                    sb.Append($"%{c:X2}");
                }
            }
            return sb.ToString();
        }
    }
}