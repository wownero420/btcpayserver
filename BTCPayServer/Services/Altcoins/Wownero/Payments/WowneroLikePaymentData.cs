#if ALTCOINS
using BTCPayServer.Client.Models;
using BTCPayServer.Payments;
using BTCPayServer.Services.Altcoins.Wownero.Utils;
using BTCPayServer.Services.Invoices;

namespace BTCPayServer.Services.Altcoins.Wownero.Payments
{
    public class WowneroLikePaymentData : CryptoPaymentData
    {
        public long Amount { get; set; }
        public string Address { get; set; }
        public long SubaddressIndex { get; set; }
        public long SubaccountIndex { get; set; }
        public long BlockHeight { get; set; }
        public long ConfirmationCount { get; set; }
        public string TransactionId { get; set; }

        public BTCPayNetworkBase Network { get; set; }

        public string GetPaymentId()
        {
            return $"{TransactionId}#{SubaccountIndex}#{SubaddressIndex}";
        }

        public string[] GetSearchTerms()
        {
            return new[] { TransactionId };
        }

        public decimal GetValue()
        {
            return WowneroMoney.Convert(Amount);
        }

        public bool PaymentCompleted(PaymentEntity entity)
        {
            return ConfirmationCount >= (Network as WowneroLikeSpecificBtcPayNetwork).MaxTrackedConfirmation;
        }

        public bool PaymentConfirmed(PaymentEntity entity, SpeedPolicy speedPolicy)
        {
            switch (speedPolicy)
            {
                case SpeedPolicy.HighSpeed:
                    return ConfirmationCount >= 0;
                case SpeedPolicy.MediumSpeed:
                    return ConfirmationCount >= 1;
                case SpeedPolicy.LowMediumSpeed:
                    return ConfirmationCount >= 2;
                case SpeedPolicy.LowSpeed:
                    return ConfirmationCount >= 6;
                default:
                    return false;
            }
        }

        public PaymentType GetPaymentType()
        {
            return WowneroPaymentType.Instance;
        }

        public string GetDestination()
        {
            return Address;
        }
    }
}
#endif
