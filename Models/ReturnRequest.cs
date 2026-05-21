using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class ReturnRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        [Required]
        public int OrderItemId { get; set; }
        public virtual OrderItem OrderItem { get; set; }

        [Required]
        [StringLength(50)]
        public string ReturnReason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? DetailedReason { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public DateTime? ApprovedDate { get; set; }

        public DateTime? ReceivedDate { get; set; }

        public DateTime? RefundedDate { get; set; }

        [StringLength(100)]
        public string? ReturnAuthorizationNumber { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal RefundAmount { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        public bool IsRefundIssued { get; set; } = false;

        [StringLength(500)]
        public string? PhotoPath1 { get; set; }

        [StringLength(500)]
        public string? PhotoPath2 { get; set; }

        [StringLength(500)]
        public string? PhotoPath3 { get; set; }
    }
}