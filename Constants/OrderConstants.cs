namespace WebApplication1.Constants
{
    /// <summary>
    /// Constants for Order, Payment, and Return statuses
    /// Replaces magic strings throughout the application
    /// </summary>
    public static class OrderConstants
    {
        /// <summary>
        /// Order Status values
        /// </summary>
        public static class OrderStatus
        {
            public const string Pending = "Pending";
            public const string Processing = "Processing";
            public const string Shipped = "Shipped";
            public const string Delivered = "Delivered";
            public const string Cancelled = "Cancelled";

            public static readonly string[] AllStatuses =
            {
                Pending,
                Processing,
                Shipped,
                Delivered,
                Cancelled
            };

            public static bool IsValid(string status)
            {
                return AllStatuses.Contains(status);
            }
        }

        /// <summary>
        /// Payment Status values
        /// </summary>
        public static class PaymentStatus
        {
            public const string Pending = "Pending";
            public const string Paid = "Paid";
            public const string Failed = "Failed";
            public const string Refunded = "Refunded";
            public const string PartiallyRefunded = "Partially Refunded";

            public static readonly string[] AllStatuses =
            {
                Pending,
                Paid,
                Failed,
                Refunded,
                PartiallyRefunded
            };

            public static bool IsValid(string status)
            {
                return AllStatuses.Contains(status);
            }
        }

        /// <summary>
        /// Payment Method values
        /// </summary>
        public static class PaymentMethod
        {
            public const string CreditCard = "Credit Card";
            public const string DebitCard = "Debit Card";
            public const string PayPal = "PayPal";
            public const string PayFast = "PayFast";
            public const string EFT = "EFT";
            public const string CashOnDelivery = "Cash on Delivery";

            public static readonly string[] AllMethods =
            {
                CreditCard,
                DebitCard,
                PayPal,
                PayFast,
                EFT,
                CashOnDelivery
            };
        }

        /// <summary>
        /// Return Request Status values
        /// </summary>
        public static class ReturnStatus
        {
            public const string Requested = "Requested";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Received = "Received";
            public const string Refunded = "Refunded";
            public const string Completed = "Completed";

            public static readonly string[] AllStatuses =
            {
                Requested,
                Approved,
                Rejected,
                Received,
                Refunded,
                Completed
            };

            public static bool IsValid(string status)
            {
                return AllStatuses.Contains(status);
            }
        }

        /// <summary>
        /// Shipping Carriers (South African focus)
        /// </summary>
        public static class Carrier
        {
            public const string TheCourierGuy = "The Courier Guy";
            public const string DHL = "DHL";
            public const string FedEx = "FedEx";
            public const string UPS = "UPS";
            public const string SAPO = "SA Post Office";
            public const string Aramex = "Aramex";
            public const string Dawn = "Dawn Wing";

            public static readonly string[] AllCarriers =
            {
                TheCourierGuy,
                DHL,
                FedEx,
                UPS,
                SAPO,
                Aramex,
                Dawn
            };
        }
    }
}