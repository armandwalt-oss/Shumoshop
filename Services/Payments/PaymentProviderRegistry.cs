namespace WebApplication1.Services.Payments
{
    public interface IPaymentProviderRegistry
    {
        IReadOnlyList<IPaymentProvider> AllProviders { get; }

        /// <summary>
        /// Providers that are configured + feature-flag-enabled AND support
        /// the requested currency. Drives the payment-method picker at checkout.
        /// </summary>
        Task<List<IPaymentProvider>> AvailableForAsync(string currencyCode);

        IPaymentProvider? Get(string providerKey);
    }

    public class PaymentProviderRegistry : IPaymentProviderRegistry
    {
        private readonly IReadOnlyList<IPaymentProvider> _providers;

        public PaymentProviderRegistry(IEnumerable<IPaymentProvider> providers)
        {
            _providers = providers.ToList();
        }

        public IReadOnlyList<IPaymentProvider> AllProviders => _providers;

        public async Task<List<IPaymentProvider>> AvailableForAsync(string currencyCode)
        {
            var available = new List<IPaymentProvider>();
            foreach (var p in _providers)
            {
                if (!p.SupportsCurrency(currencyCode)) continue;
                if (!await p.IsAvailableAsync()) continue;
                available.Add(p);
            }
            return available;
        }

        public IPaymentProvider? Get(string providerKey) =>
            _providers.FirstOrDefault(p =>
                string.Equals(p.ProviderKey, providerKey, StringComparison.OrdinalIgnoreCase));
    }
}
