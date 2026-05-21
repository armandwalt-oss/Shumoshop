using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Helpers;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Admin-only actions for The Courier Guy: book a shipment, fetch rates,
    /// track, download waybill, cancel — plus the AllowAnonymous webhook
    /// endpoint that TCG calls when a tracking event fires.
    ///
    /// Ported (and simplified) from CXI's Areas/Admin/CourierGuyController.
    /// </summary>
    [Authorize(Roles = "Admin,Dev")]
    [AutoValidateAntiforgeryToken]
    public class CourierGuyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICourierGuyService _tcg;
        private readonly ILogger<CourierGuyController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        private TcgAddress DispatchAddress => ShippingAddressHelper.GetDispatchAddress(_configuration, _logger);
        private TcgContact DispatchContact => ShippingAddressHelper.GetDispatchContact(_configuration);

        public CourierGuyController(
            ApplicationDbContext context,
            ICourierGuyService tcg,
            ILogger<CourierGuyController> logger,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _context = context;
            _tcg = tcg;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
        }

        // ── Book Collection ──────────────────────────────────────────────────
        // Called from the admin order details view's "Book Collection" button.
        [HttpPost]
        public async Task<IActionResult> BookCollection([FromBody] BookCollectionRequest model)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId);

                if (order is null)
                    return Json(new { success = false, message = "Order not found." });

                if (string.IsNullOrWhiteSpace(order.ShippingAddress))
                    return Json(new { success = false, message = "Order has no shipping address. Cannot book collection." });

                var deliveryContact = new TcgContact
                {
                    Name = $"{order.CustomerName} {order.CustomerSurname}".Trim(),
                    MobileNumber = order.PhoneNumber ?? "",
                    Email = order.Email ?? ""
                };

                var deliveryAddress = new TcgAddress
                {
                    Type = "residential",
                    StreetAddress = order.ShippingAddress,
                    LocalArea = order.ShippingCity,
                    City = order.ShippingCity,
                    Zone = order.ShippingState,
                    Country = "ZA",
                    Code = order.ShippingPostalCode
                };

                var parcel = new TcgParcel
                {
                    ParcelDescription = $"ShumoShop Order {order.OrderNumber}",
                    LengthCm = model.LengthCm > 0 ? model.LengthCm : 30,
                    WidthCm = model.WidthCm > 0 ? model.WidthCm : 20,
                    HeightCm = model.HeightCm > 0 ? model.HeightCm : 10,
                    WeightKg = model.WeightKg > 0 ? model.WeightKg : 1
                };

                var request = new TcgCreateShipmentRequest
                {
                    CollectionAddress = DispatchAddress,
                    CollectionContact = DispatchContact,
                    DeliveryAddress = deliveryAddress,
                    DeliveryContact = deliveryContact,
                    Parcels = new List<TcgParcel> { parcel },
                    ServiceLevelCode = model.ServiceLevelCode ?? "ECO",
                    CustomerReferenceName = "Order no.",
                    CustomerReference = order.OrderNumber,
                    DeclaredValue = order.TotalAmount,
                    CollectionMinDate = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00Z"),
                    MuteNotifications = false
                };

                var result = await _tcg.CreateShipmentAsync(request);
                if (!result.Success)
                    return Json(new { success = false, message = result.ErrorMessage });

                var shipment = result.Data!;

                order.TrackingNumber = shipment.CustomTrackingReference ?? shipment.ShortTrackingReference;
                order.Carrier = "The Courier Guy";
                if (order.Status == "Pending") order.Status = "Processing";

                // Stash TCG shipment ID in Notes so we can fetch the waybill later.
                var stamp = $"[TCG Shipment ID: {shipment.Id}]";
                order.Notes = string.IsNullOrEmpty(order.Notes) ? stamp : $"{stamp}\n{order.Notes}";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Collection booked. Tracking: {order.TrackingNumber}",
                    trackingReference = order.TrackingNumber,
                    shipmentId = shipment.Id,
                    estimatedDeliveryFrom = shipment.EstimatedDeliveryFrom?.ToString("dd MMM yyyy"),
                    estimatedDeliveryTo = shipment.EstimatedDeliveryTo?.ToString("dd MMM yyyy"),
                    rate = shipment.Rate?.ToString("N2"),
                    serviceLevelName = shipment.ServiceLevelName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to book TCG collection for order {OrderId}.", model.OrderId);
                return Json(new { success = false, message = "Could not book the collection. See server log." });
            }
        }

        // ── Get Rates ────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> GetRates([FromBody] GetRatesRequest model)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId);
                if (order is null) return Json(new { success = false, message = "Order not found." });
                if (string.IsNullOrWhiteSpace(order.ShippingAddress))
                    return Json(new { success = false, message = "Order has no shipping address." });

                var deliveryAddress = new TcgAddress
                {
                    Type = "residential",
                    StreetAddress = order.ShippingAddress,
                    LocalArea = order.ShippingCity,
                    City = order.ShippingCity,
                    Zone = order.ShippingState,
                    Country = "ZA",
                    Code = order.ShippingPostalCode
                };

                var parcel = new TcgParcel
                {
                    LengthCm = model.LengthCm > 0 ? model.LengthCm : 30,
                    WidthCm = model.WidthCm > 0 ? model.WidthCm : 20,
                    HeightCm = model.HeightCm > 0 ? model.HeightCm : 10,
                    WeightKg = model.WeightKg > 0 ? model.WeightKg : 1
                };

                var result = await _tcg.GetRatesAsync(
                    DispatchAddress, deliveryAddress,
                    new List<TcgParcel> { parcel },
                    order.TotalAmount);

                if (!result.Success) return Json(new { success = false, message = result.ErrorMessage });

                var rates = result.Data!.Select(r => new
                {
                    serviceLevelCode = r.ServiceLevelCode,
                    serviceLevelName = r.ServiceLevelName,
                    rate = r.Rate.ToString("N2"),
                    rateExVat = r.RateExcludingVat.ToString("N2"),
                    deliveryFrom = r.DeliveryDateFrom?.ToString("dd MMM yyyy HH:mm"),
                    deliveryTo = r.DeliveryDateTo?.ToString("dd MMM yyyy HH:mm"),
                    cutOff = r.ServiceLevel?.CollectionCutOffTime?.ToString("dd MMM HH:mm")
                });

                return Json(new { success = true, rates });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch TCG rates for order {OrderId}.", model.OrderId);
                return Json(new { success = false, message = "Could not fetch rates. See server log." });
            }
        }

        // ── Track Shipment ───────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Track(string trackingReference)
        {
            try
            {
                var result = await _tcg.TrackShipmentAsync(trackingReference);
                if (!result.Success) return Json(new { success = false, message = result.ErrorMessage });

                var tracking = result.Data!;
                return Json(new
                {
                    success = true,
                    status = tracking.Status,
                    statusLabel = MapStatusLabel(tracking.Status),
                    estimatedDeliveryFrom = tracking.EstimatedDeliveryFrom?.ToString("dd MMM yyyy"),
                    estimatedDeliveryTo = tracking.EstimatedDeliveryTo?.ToString("dd MMM yyyy"),
                    events = tracking.TrackingEvents
                        .OrderByDescending(e => e.Date)
                        .Select(e => new
                        {
                            date = e.Date.ToString("dd MMM yyyy HH:mm"),
                            status = e.Status,
                            label = MapStatusLabel(e.Status),
                            message = e.Message,
                            location = e.Location
                        })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track TCG shipment {TrackingReference}.", trackingReference);
                return Json(new { success = false, message = "Could not track the shipment." });
            }
        }

        // ── Download Waybill ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Waybill(int shipmentId)
        {
            try
            {
                var result = await _tcg.GetWaybillUrlAsync(shipmentId);
                if (!result.Success) return Json(new { success = false, message = result.ErrorMessage });
                return Redirect(result.Data!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch waybill for TCG shipment {ShipmentId}.", shipmentId);
                return Json(new { success = false, message = "Could not fetch the waybill." });
            }
        }

        // ── Cancel Shipment ──────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CancelShipment([FromBody] CancelShipmentRequest model)
        {
            try
            {
                var result = await _tcg.CancelShipmentAsync(model.ShipmentId);
                if (!result.Success) return Json(new { success = false, message = result.ErrorMessage });

                var order = await _context.Orders.FindAsync(model.OrderId);
                if (order != null)
                {
                    order.TrackingNumber = null;
                    order.Carrier = null;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Shipment cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel TCG shipment {ShipmentId} for order {OrderId}.",
                    model.ShipmentId, model.OrderId);
                return Json(new { success = false, message = "Could not cancel the shipment." });
            }
        }

        // ── Webhook receiver ─────────────────────────────────────────────────
        // Configure in TCG portal:
        //   Topic        : Tracking event
        //   Delivery URL : https://yourdomain/CourierGuy/Webhook
        //   Auth key     : same value as CourierGuy:WebhookAuthKey in config
        //
        // TCG sends the Auth key as "Authorization: Bearer <key>". We reject
        // anything without a matching bearer token so a leaked URL alone
        // can't spoof order status changes.
        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook([FromBody] TcgWebhookPayload payload)
        {
            try
            {
                // 1. Authenticate
                var expectedKey = _configuration["CourierGuy:WebhookAuthKey"];
                if (string.IsNullOrWhiteSpace(expectedKey))
                {
                    _logger.LogError("TCG webhook: CourierGuy:WebhookAuthKey is not configured. Rejecting.");
                    return Unauthorized();
                }

                var authHeader = Request.Headers.Authorization.ToString();
                if (string.IsNullOrEmpty(authHeader) ||
                    !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("TCG webhook: missing or malformed Authorization header from {Ip}.",
                        HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized();
                }

                var presented = authHeader.Substring("Bearer ".Length).Trim();

                // Constant-time compare to defeat timing attacks.
                var presentedBytes = Encoding.UTF8.GetBytes(presented);
                var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);
                if (presentedBytes.Length != expectedBytes.Length ||
                    !CryptographicOperations.FixedTimeEquals(presentedBytes, expectedBytes))
                {
                    _logger.LogWarning("TCG webhook: Auth key mismatch from {Ip}.",
                        HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized();
                }

                // 2. Parse / sanity-check
                if (string.IsNullOrEmpty(payload?.CustomTrackingReference))
                    return Ok();

                // 3. Find matching order
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.TrackingNumber == payload.CustomTrackingReference);

                if (order is null)
                {
                    _logger.LogInformation("TCG webhook: no order for tracking {Ref}. Status {Status}.",
                        payload.CustomTrackingReference, payload.Status);
                    return Ok();
                }

                // 4. Map TCG status → ShumoShop status
                var newStatus = MapTcgStatus(payload.Status);
                if (newStatus is not null)
                {
                    var previousStatus = order.Status;
                    order.Status = newStatus;

                    bool firstShippedTransition = newStatus == "Shipped" && !order.ShippedDate.HasValue;
                    bool firstDeliveredTransition = newStatus == "Delivered" && !order.DeliveredDate.HasValue;

                    if (firstShippedTransition) order.ShippedDate = DateTime.Now;
                    if (firstDeliveredTransition) order.DeliveredDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "TCG webhook: order {OrderNumber} ({Tracking}) → {Status}.",
                        order.OrderNumber, payload.CustomTrackingReference, newStatus);

                    // Tell the customer when the status actually moves to a
                    // milestone — best-effort, never break the webhook on a
                    // mail failure.
                    try
                    {
                        var customer = await _context.Users.FindAsync(order.UserId);
                        if (customer != null)
                        {
                            if (firstShippedTransition)
                                await _emailService.SendShippingNotificationAsync(order, customer);
                            else if (firstDeliveredTransition)
                                await _emailService.SendDeliveryConfirmationAsync(order, customer);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx,
                            "TCG webhook: failed to send status email for order {OrderNumber}",
                            order.OrderNumber);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                // Always 200 so TCG doesn't retry — log loudly for ops.
                _logger.LogError(ex, "TCG webhook processing failed.");
                return Ok();
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string? MapTcgStatus(string? status) => status switch
        {
            "collected" => "Processing",
            "at-hub" => "Processing",
            "in-transit" => "Shipped",
            "at-destination-hub" => "Shipped",
            "out-for-delivery" => "Shipped",
            "delivered" => "Delivered",
            "returned-to-sender" => "Cancelled",
            "cancelled" => "Cancelled",
            _ => null
        };

        private static string MapStatusLabel(string? status) => status switch
        {
            "submitted" => "Submitted",
            "collection-assigned" => "Driver Assigned for Collection",
            "collection-unassigned" => "Awaiting Driver",
            "collected" => "Collected",
            "collection-failed-attempt" => "Collection Attempt Failed",
            "collection-exception" => "Collection Exception",
            "at-hub" => "At Hub",
            "in-transit" => "In Transit",
            "at-destination-hub" => "At Destination Hub",
            "out-for-delivery" => "Out for Delivery",
            "delivery-assigned" => "Driver Assigned for Delivery",
            "delivery-failed-attempt" => "Delivery Attempt Failed",
            "delivery-exception" => "Delivery Exception",
            "delivered" => "Delivered",
            "returned-to-sender" => "Returned to Sender",
            "on-hold" => "On Hold",
            "cancelled" => "Cancelled",
            _ => status ?? "Unknown"
        };
    }

    // ── Request models ───────────────────────────────────────────────────────

    public class BookCollectionRequest
    {
        public int OrderId { get; set; }
        public double LengthCm { get; set; }
        public double WidthCm { get; set; }
        public double HeightCm { get; set; }
        public double WeightKg { get; set; }
        public string? ServiceLevelCode { get; set; }
    }

    public class GetRatesRequest
    {
        public int OrderId { get; set; }
        public double LengthCm { get; set; }
        public double WidthCm { get; set; }
        public double HeightCm { get; set; }
        public double WeightKg { get; set; }
    }

    public class CancelShipmentRequest
    {
        public int OrderId { get; set; }
        public int ShipmentId { get; set; }
    }

    public class TcgWebhookPayload
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("customer_reference")]
        public string? CustomerReference { get; set; }

        [JsonPropertyName("custom_tracking_reference")]
        public string? CustomTrackingReference { get; set; }

        [JsonPropertyName("id")]
        public int ShipmentId { get; set; }
    }
}
