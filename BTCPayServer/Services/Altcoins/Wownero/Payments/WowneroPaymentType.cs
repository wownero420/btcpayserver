#if ALTCOINS
using System.Globalization;
using BTCPayServer.Payments;
using BTCPayServer.Services.Invoices;
using NBitcoin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BTCPayServer.Services.Altcoins.Wownero.Payments
{
    public class WowneroPaymentType : PaymentType
    {
        public static WowneroPaymentType Instance { get; } = new WowneroPaymentType();
        public override string ToPrettyString() => "On-Chain";

        public override string GetId() => "WowneroLike";
        public override string ToStringNormalized()
        {
            return "Wownero";
        }

        public override CryptoPaymentData DeserializePaymentData(BTCPayNetworkBase network, string str)
        {
            return JsonConvert.DeserializeObject<WowneroLikePaymentData>(str);
        }

        public override string SerializePaymentData(BTCPayNetworkBase network, CryptoPaymentData paymentData)
        {
            return JsonConvert.SerializeObject(paymentData);
        }

        public override IPaymentMethodDetails DeserializePaymentMethodDetails(BTCPayNetworkBase network, string str)
        {
            return JsonConvert.DeserializeObject<WowneroLikeOnChainPaymentMethodDetails>(str);
        }

        public override string SerializePaymentMethodDetails(BTCPayNetworkBase network, IPaymentMethodDetails details)
        {
            return JsonConvert.SerializeObject(details);
        }

        public override ISupportedPaymentMethod DeserializeSupportedPaymentMethod(BTCPayNetworkBase network, JToken value)
        {
            return JsonConvert.DeserializeObject<WowneroSupportedPaymentMethod>(value.ToString());
        }

        public override string GetTransactionLink(BTCPayNetworkBase network, string txId)
        {
            return string.Format(CultureInfo.InvariantCulture, network.BlockExplorerLink, txId);
        }

        public override string GetPaymentLink(BTCPayNetworkBase network, IPaymentMethodDetails paymentMethodDetails, Money cryptoInfoDue, string serverUri)
        {
            return paymentMethodDetails.Activated
                ? $"{(network as WowneroLikeSpecificBtcPayNetwork).UriScheme}:{paymentMethodDetails.GetPaymentDestination()}?tx_amount={cryptoInfoDue.ToDecimal(MoneyUnit.BTC)}"
                : string.Empty;
        }

        public override string InvoiceViewPaymentPartialName { get; } = "Wownero/ViewWowneroLikePaymentData";
        public override object GetGreenfieldData(ISupportedPaymentMethod supportedPaymentMethod, bool canModifyStore)
        {
            if (supportedPaymentMethod is WowneroSupportedPaymentMethod wowneroSupportedPaymentMethod)
            {
                return new
                {
                    wowneroSupportedPaymentMethod.AccountIndex,
                };
            }

            return null;
        }

        public override void PopulateCryptoInfo(PaymentMethod details, InvoiceCryptoInfo invoiceCryptoInfo, string serverUrl)
        {
            
        }
    }
}
#endif
