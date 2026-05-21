using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication1.Services
{
    // ── Response / request models ────────────────────────────────────────────
    // Ported from the CXI project. These mirror The Courier Guy (TCG) API
    // request/response shapes. Property names use snake_case via
    // JsonPropertyName so we keep idiomatic C# in the rest of the codebase.

    public class TcgAddress
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "residential";

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("street_address")]
        public string StreetAddress { get; set; } = string.Empty;

        [JsonPropertyName("local_area")]
        public string LocalArea { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("zone")]
        public string Zone { get; set; } = string.Empty;

        [JsonPropertyName("country")]
        public string Country { get; set; } = "ZA";

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        // Optional geo coords (used by /pickup-points and helpful for rate
        // accuracy on metro deliveries). Null = TCG geocodes server-side.
        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lng")]
        public double? Lng { get; set; }
    }

    // A single locker or kiosk returned by GET /pickup-points
    public class TcgPickupPoint
    {
        [JsonPropertyName("pickup_point_id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }  // "locker" | "counter"

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }  // typically "tcg-locker"

        [JsonPropertyName("address")]
        public TcgAddress? Address { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lng")]
        public double? Lng { get; set; }

        [JsonPropertyName("distance_km")]
        public double? DistanceKm { get; set; }

        [JsonPropertyName("opening_hours")]
        public string? OpeningHours { get; set; }
    }

    public class TcgPickupPointsResponse
    {
        [JsonPropertyName("pickup_points")]
        public List<TcgPickupPoint> PickupPoints { get; set; } = new();
    }

    public class TcgContact
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("mobile_number")]
        public string MobileNumber { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class TcgParcel
    {
        [JsonPropertyName("parcel_description")]
        public string ParcelDescription { get; set; } = "Automotive clips and fasteners";

        [JsonPropertyName("submitted_length_cm")]
        public double LengthCm { get; set; } = 30;

        [JsonPropertyName("submitted_width_cm")]
        public double WidthCm { get; set; } = 20;

        [JsonPropertyName("submitted_height_cm")]
        public double HeightCm { get; set; } = 10;

        [JsonPropertyName("submitted_weight_kg")]
        public double WeightKg { get; set; } = 1;
    }

    public class TcgCreateShipmentRequest
    {
        [JsonPropertyName("collection_address")]
        public TcgAddress? CollectionAddress { get; set; }

        [JsonPropertyName("collection_pickup_point_id")]
        public string? CollectionPickupPointId { get; set; }

        [JsonPropertyName("collection_pickup_point_provider")]
        public string? CollectionPickupPointProvider { get; set; }

        [JsonPropertyName("collection_contact")]
        public TcgContact CollectionContact { get; set; } = new();

        [JsonPropertyName("delivery_address")]
        public TcgAddress? DeliveryAddress { get; set; }

        [JsonPropertyName("delivery_pickup_point_id")]
        public string? DeliveryPickupPointId { get; set; }

        [JsonPropertyName("delivery_pickup_point_provider")]
        public string? DeliveryPickupPointProvider { get; set; }

        [JsonPropertyName("delivery_contact")]
        public TcgContact DeliveryContact { get; set; } = new();

        [JsonPropertyName("parcels")]
        public List<TcgParcel> Parcels { get; set; } = new();

        [JsonPropertyName("service_level_code")]
        public string ServiceLevelCode { get; set; } = "ECO";

        [JsonPropertyName("customer_reference_name")]
        public string CustomerReferenceName { get; set; } = "Order no.";

        [JsonPropertyName("customer_reference")]
        public string CustomerReference { get; set; } = string.Empty;

        [JsonPropertyName("special_instructions_collection")]
        public string? SpecialInstructionsCollection { get; set; }

        [JsonPropertyName("special_instructions_delivery")]
        public string? SpecialInstructionsDelivery { get; set; }

        [JsonPropertyName("declared_value")]
        public decimal DeclaredValue { get; set; } = 0;

        [JsonPropertyName("collection_min_date")]
        public string CollectionMinDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00Z");

        [JsonPropertyName("mute_notifications")]
        public bool MuteNotifications { get; set; } = false;

        [JsonPropertyName("opt_in_rates")]
        public List<int> OptInRates { get; set; } = new();
    }

    public class TcgShipmentResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("custom_tracking_reference")]
        public string? CustomTrackingReference { get; set; }

        [JsonPropertyName("short_tracking_reference")]
        public string? ShortTrackingReference { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("service_level_code")]
        public string? ServiceLevelCode { get; set; }

        [JsonPropertyName("service_level_name")]
        public string? ServiceLevelName { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("estimated_delivery_from")]
        public DateTime? EstimatedDeliveryFrom { get; set; }

        [JsonPropertyName("estimated_delivery_to")]
        public DateTime? EstimatedDeliveryTo { get; set; }
    }

    public class TcgServiceLevel
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("delivery_date_from")]
        public DateTime? DeliveryDateFrom { get; set; }

        [JsonPropertyName("delivery_date_to")]
        public DateTime? DeliveryDateTo { get; set; }

        [JsonPropertyName("collection_date")]
        public DateTime? CollectionDate { get; set; }

        [JsonPropertyName("collection_cut_off_time")]
        public DateTime? CollectionCutOffTime { get; set; }
    }

    public class TcgRateResult
    {
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("rate_excluding_vat")]
        public decimal RateExcludingVat { get; set; }

        [JsonPropertyName("service_level")]
        public TcgServiceLevel? ServiceLevel { get; set; }

        public string? ServiceLevelCode => ServiceLevel?.Code;
        public string? ServiceLevelName => ServiceLevel?.Name;
        public DateTime? DeliveryDateFrom => ServiceLevel?.DeliveryDateFrom;
        public DateTime? DeliveryDateTo => ServiceLevel?.DeliveryDateTo;
    }

    public class TcgRatesResponse
    {
        [JsonPropertyName("rates")]
        public List<TcgRateResult> Rates { get; set; } = new();
    }

    public class TcgTrackingEvent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }
    }

    public class TcgTrackingResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("custom_tracking_reference")]
        public string? CustomTrackingReference { get; set; }

        [JsonPropertyName("estimated_delivery_from")]
        public DateTime? EstimatedDeliveryFrom { get; set; }

        [JsonPropertyName("estimated_delivery_to")]
        public DateTime? EstimatedDeliveryTo { get; set; }

        [JsonPropertyName("tracking_events")]
        public List<TcgTrackingEvent> TrackingEvents { get; set; } = new();
    }

    public class TcgServiceResult<T>
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public T? Data { get; set; }
        public string? RawJson { get; set; }
    }

    // ── Service ──────────────────────────────────────────────────────────────

    public interface ICourierGuyService
    {
        Task<TcgServiceResult<TcgShipmentResponse>> CreateShipmentAsync(TcgCreateShipmentRequest request);

        // Door-to-Door rate.
        Task<TcgServiceResult<List<TcgRateResult>>> GetRatesAsync(
            TcgAddress collection, TcgAddress delivery,
            List<TcgParcel> parcels, decimal declaredValue = 0);

        // Rate for a delivery to a specific locker/kiosk.
        Task<TcgServiceResult<List<TcgRateResult>>> GetRatesToPickupPointAsync(
            TcgAddress collection, string deliveryPickupPointId,
            List<TcgParcel> parcels, decimal declaredValue = 0);

        // List lockers OR kiosks. type = "locker" or "counter".
        Task<TcgServiceResult<List<TcgPickupPoint>>> GetPickupPointsAsync(
            string type, double? lat = null, double? lng = null,
            string? search = null, bool orderClosest = true);

        Task<TcgServiceResult<TcgTrackingResponse>> TrackShipmentAsync(string trackingReference);
        Task<TcgServiceResult<string>> GetWaybillUrlAsync(int shipmentId);
        Task<TcgServiceResult<bool>> CancelShipmentAsync(int shipmentId);
    }

    public class CourierGuyService : ICourierGuyService
    {
        private const string BaseUrl = "https://api.portal.thecourierguy.co.za";
        private readonly HttpClient _http;
        private readonly ILogger<CourierGuyService> _logger;
        private readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public CourierGuyService(HttpClient http, IConfiguration config, ILogger<CourierGuyService> logger)
        {
            _http = http;
            _logger = logger;

            // Don't throw on startup when the API key isn't configured yet —
            // the rest of the app should still boot in dev. Calls will fail
            // gracefully via the TcgServiceResult pattern.
            var apiKey = config["CourierGuy:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("CourierGuy:ApiKey is not configured. Calls to The Courier Guy will fail until a key is set in user-secrets or appsettings.Production.json.");
            }

            _http.BaseAddress = new Uri(BaseUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
            _http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ── Create Shipment ──────────────────────────────────────────────────

        public async Task<TcgServiceResult<TcgShipmentResponse>> CreateShipmentAsync(
            TcgCreateShipmentRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, _jsonOpts);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/shipments", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TCG CreateShipment failed {Status}: {Body}", response.StatusCode, body);
                    return Fail<TcgShipmentResponse>($"API error {(int)response.StatusCode}: {body}");
                }

                var result = JsonSerializer.Deserialize<TcgShipmentResponse>(body, _jsonOpts);
                return Ok(result!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG CreateShipment exception");
                return Fail<TcgShipmentResponse>(ex.Message);
            }
        }

        // ── Get Rates ────────────────────────────────────────────────────────

        public async Task<TcgServiceResult<List<TcgRateResult>>> GetRatesAsync(
            TcgAddress collection, TcgAddress delivery,
            List<TcgParcel> parcels, decimal declaredValue = 0)
        {
            try
            {
                var payload = new
                {
                    collection_address = collection,
                    delivery_address = delivery,
                    parcels,
                    declared_value = declaredValue,
                    collection_min_date = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00Z")
                };

                var json = JsonSerializer.Serialize(payload, _jsonOpts);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/rates", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TCG GetRates failed {Status}: {Body}", response.StatusCode, body);
                    return Fail<List<TcgRateResult>>($"API error {(int)response.StatusCode}: {body}");
                }

                var wrapper = JsonSerializer.Deserialize<TcgRatesResponse>(body, _jsonOpts);
                var rates = wrapper?.Rates ?? new();
                return new TcgServiceResult<List<TcgRateResult>> { Success = true, Data = rates, RawJson = body };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG GetRates exception");
                return Fail<List<TcgRateResult>>(ex.Message);
            }
        }

        // ── Get Rates to a pickup point ──────────────────────────────────────

        public async Task<TcgServiceResult<List<TcgRateResult>>> GetRatesToPickupPointAsync(
            TcgAddress collection, string deliveryPickupPointId,
            List<TcgParcel> parcels, decimal declaredValue = 0)
        {
            try
            {
                var payload = new
                {
                    collection_address = collection,
                    delivery_pickup_point_id = deliveryPickupPointId,
                    delivery_pickup_point_provider = "tcg-locker",
                    parcels,
                    declared_value = declaredValue,
                    collection_min_date = DateTime.UtcNow.ToString("yyyy-MM-ddT00:00:00Z")
                };

                var json = JsonSerializer.Serialize(payload, _jsonOpts);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/rates", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TCG GetRatesToPickupPoint failed {Status}: {Body}", response.StatusCode, body);
                    return Fail<List<TcgRateResult>>($"API error {(int)response.StatusCode}: {body}");
                }

                var wrapper = JsonSerializer.Deserialize<TcgRatesResponse>(body, _jsonOpts);
                var rates = wrapper?.Rates ?? new();
                return new TcgServiceResult<List<TcgRateResult>> { Success = true, Data = rates, RawJson = body };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG GetRatesToPickupPoint exception");
                return Fail<List<TcgRateResult>>(ex.Message);
            }
        }

        // ── List pickup points (lockers / kiosks) ────────────────────────────

        public async Task<TcgServiceResult<List<TcgPickupPoint>>> GetPickupPointsAsync(
            string type, double? lat = null, double? lng = null,
            string? search = null, bool orderClosest = true)
        {
            try
            {
                var qs = new List<string> { "type=" + Uri.EscapeDataString(type) };
                if (lat.HasValue && lng.HasValue)
                {
                    qs.Add("lat=" + lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    qs.Add("lng=" + lng.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if (orderClosest) qs.Add("order_closest=true");
                }
                if (!string.IsNullOrWhiteSpace(search))
                    qs.Add("search=" + Uri.EscapeDataString(search));

                var url = "https://api.shiplogic.com/pickup-points?" + string.Join("&", qs);
                var response = await _http.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TCG GetPickupPoints failed {Status}: {Body}", response.StatusCode, body);
                    return Fail<List<TcgPickupPoint>>($"API error {(int)response.StatusCode}: {body}");
                }

                List<TcgPickupPoint> list = new();
                try
                {
                    var wrapper = JsonSerializer.Deserialize<TcgPickupPointsResponse>(body, _jsonOpts);
                    if (wrapper?.PickupPoints != null)
                        list = wrapper.PickupPoints;
                }
                catch (JsonException)
                {
                    try
                    {
                        list = JsonSerializer.Deserialize<List<TcgPickupPoint>>(body, _jsonOpts) ?? new();
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogError(jex,
                            "TCG GetPickupPoints: unexpected response shape. Body preview: {Body}",
                            body.Length > 400 ? body.Substring(0, 400) : body);
                        return Fail<List<TcgPickupPoint>>("Unexpected response from TCG. See server log for the raw body.");
                    }
                }

                return new TcgServiceResult<List<TcgPickupPoint>> { Success = true, Data = list, RawJson = body };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG GetPickupPoints exception");
                return Fail<List<TcgPickupPoint>>(ex.Message);
            }
        }

        // ── Track Shipment ───────────────────────────────────────────────────

        public async Task<TcgServiceResult<TcgTrackingResponse>> TrackShipmentAsync(string trackingReference)
        {
            try
            {
                var response = await _http.GetAsync(
                    $"/tracking/shipments?tracking_reference={Uri.EscapeDataString(trackingReference)}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail<TcgTrackingResponse>($"API error {(int)response.StatusCode}: {body}");

                var result = JsonSerializer.Deserialize<TcgTrackingResponse>(body, _jsonOpts);
                return Ok(result!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG TrackShipment exception");
                return Fail<TcgTrackingResponse>(ex.Message);
            }
        }

        // ── Get Waybill PDF URL ──────────────────────────────────────────────

        public async Task<TcgServiceResult<string>> GetWaybillUrlAsync(int shipmentId)
        {
            try
            {
                var response = await _http.GetAsync($"/shipments/label?id={shipmentId}");
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail<string>($"API error {(int)response.StatusCode}: {body}");

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("url", out var urlProp))
                {
                    var url = urlProp.GetString();
                    if (!string.IsNullOrEmpty(url))
                        return Ok(url);
                }

                var plain = body.Trim('"', ' ');
                return Ok(plain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG GetWaybillUrl exception");
                return Fail<string>(ex.Message);
            }
        }

        // ── Cancel Shipment ──────────────────────────────────────────────────

        public async Task<TcgServiceResult<bool>> CancelShipmentAsync(int shipmentId)
        {
            try
            {
                var payload = new { shipment_id = shipmentId };
                var json = JsonSerializer.Serialize(payload, _jsonOpts);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("/shipments/cancel", content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Fail<bool>($"API error {(int)response.StatusCode}: {body}");

                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG CancelShipment exception");
                return Fail<bool>(ex.Message);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static TcgServiceResult<T> Ok<T>(T data) =>
            new() { Success = true, Data = data };

        private static TcgServiceResult<T> Fail<T>(string error) =>
            new() { Success = false, ErrorMessage = error };
    }
}
