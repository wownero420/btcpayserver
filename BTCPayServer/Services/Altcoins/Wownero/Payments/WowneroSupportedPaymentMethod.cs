#if ALTCOINS
using BTCPayServer.Payments;

namespace BTCPayServer.Services.Altcoins.Wownero.Payments
{
    public class WowneroSupportedPaymentMethod : ISupportedPaymentMethod
    {

        public string CryptoCode { get; set; }
        public long AccountIndex { get; set; }
        public PaymentMethodId PaymentId => new PaymentMethodId(CryptoCode, WowneroPaymentType.Instance);
    }
}
#endif
