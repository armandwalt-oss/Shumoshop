using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    /// <summary>
    /// Admin-editable shipping configuration. A single row (Id = 1) holds all
    /// values. The OrderController loads this on every checkout to compute
    /// the shipping cost. Defaults match the previous hardcoded behaviour
    /// (free over R500, else flat R75).
    /// </summary>
    public class ShippingSettings
    {
        public int Id { get; set; }

        /// <summary>
        /// false = use the flat <c>DoorToDoorRate</c> below.
        /// true  = fetch the live rate from The Courier Guy at order placement
        ///         and add <c>MarkupRand</c> on top. Free-shipping threshold
        ///         still applies in both modes.
        /// </summary>
        [Display(Name = "Use live Courier Guy rates")]
        public bool UseLiveTcgRates { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Door-to-Door rate (R)")]
        public decimal DoorToDoorRate { get; set; } = 75m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Door-to-Locker rate (R)")]
        public decimal DoorToLockerRate { get; set; } = 55m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Door-to-Kiosk rate (R)")]
        public decimal DoorToKioskRate { get; set; } = 55m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Free-shipping threshold (R)")]
        public decimal FreeShippingThreshold { get; set; } = 500m;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Markup per shipment (R)")]
        public decimal MarkupRand { get; set; } = 30m;

        /// <summary>
        /// Stock at or below this number is considered "low" and appears on
        /// the admin low-stock report plus a dashboard banner.
        /// </summary>
        [Display(Name = "Low-stock threshold")]
        public int LowStockThreshold { get; set; } = 10;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UpdatedByUserId { get; set; }
    }
}
