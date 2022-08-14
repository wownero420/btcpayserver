#if ALTCOINS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Logging;
using BTCPayServer.Models;
using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Payments;
using BTCPayServer.Rating;
using BTCPayServer.Services.Altcoins.Wownero.RPC.Models;
using BTCPayServer.Services.Altcoins.Wownero.Services;
using BTCPayServer.Services.Altcoins.Wownero.Utils;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Rates;
using NBitcoin;

namespace BTCPayServer.Services.Altcoins.Wownero.Payments
{
    public class WowneroLikePaymentMethodHandler : PaymentMethodHandlerBase<WowneroSupportedPaymentMethod, WowneroLikeSpecificBtcPayNetwork>
    {
        private readonly BTCPayNetworkProvider _networkProvider;
        private readonly WowneroRPCProvider _wowneroRpcProvider;

        public WowneroLikePaymentMethodHandler(BTCPayNetworkProvider networkProvider, WowneroRPCProvider wowneroRpcProvider)
        {
            _networkProvider = networkProvider;
            _wowneroRpcProvider = wowneroRpcProvider;
        }
        public override PaymentType PaymentType => WowneroPaymentType.Instance;

        public override async Task<IPaymentMethodDetails> CreatePaymentMethodDetails(InvoiceLogs logs, WowneroSupportedPaymentMethod supportedPaymentMethod, PaymentMethod paymentMethod,
            StoreData store, WowneroLikeSpecificBtcPayNetwork network, object preparePaymentObject, IEnumerable<PaymentMethodId> invoicePaymentMethods)
        {
            
            if (preparePaymentObject is null)
            {
                return new WowneroLikeOnChainPaymentMethodDetails()
                {
                    Activated = false
                };
            }

            if (!_wowneroRpcProvider.IsAvailable(network.CryptoCode))
                throw new PaymentMethodUnavailableException($"Node or wallet not available");
            var invoice = paymentMethod.ParentEntity;
            if (!(preparePaymentObject is Prepare wowneroPrepare))
                throw new ArgumentException();
            var feeRatePerKb = await wowneroPrepare.GetFeeRate;
            var address = await wowneroPrepare.ReserveAddress(invoice.Id);

            var feeRatePerByte = feeRatePerKb.Fee / 1024;
            return new WowneroLikeOnChainPaymentMethodDetails()
            {
                NextNetworkFee = WowneroMoney.Convert(feeRatePerByte * 100),
                AccountIndex = supportedPaymentMethod.AccountIndex,
                AddressIndex = address.AddressIndex,
                DepositAddress = address.Address,
                Activated = true
            };

        }

        public override object PreparePayment(WowneroSupportedPaymentMethod supportedPaymentMethod, StoreData store,
            BTCPayNetworkBase network)
        {

            var walletClient = _wowneroRpcProvider.WalletRpcClients[supportedPaymentMethod.CryptoCode];
            var daemonClient = _wowneroRpcProvider.DaemonRpcClients[supportedPaymentMethod.CryptoCode];
            return new Prepare()
            {
                GetFeeRate = daemonClient.SendCommandAsync<GetFeeEstimateRequest, GetFeeEstimateResponse>("get_fee_estimate", new GetFeeEstimateRequest()),
                ReserveAddress = s => walletClient.SendCommandAsync<CreateAddressRequest, CreateAddressResponse>("create_address", new CreateAddressRequest() { Label = $"btcpay invoice #{s}", AccountIndex = supportedPaymentMethod.AccountIndex })
            };
        }

        class Prepare
        {
            public Task<GetFeeEstimateResponse> GetFeeRate;
            public Func<string, Task<CreateAddressResponse>> ReserveAddress;
        }

        public override void PreparePaymentModel(PaymentModel model, InvoiceResponse invoiceResponse,
            StoreBlob storeBlob, IPaymentMethod paymentMethod)
        {
            var paymentMethodId = paymentMethod.GetId();
            var network = _networkProvider.GetNetwork<WowneroLikeSpecificBtcPayNetwork>(model.CryptoCode);
            model.PaymentMethodName = GetPaymentMethodName(network);
            model.CryptoImage = GetCryptoImage(network);
            if (model.Activated)
            {
                var cryptoInfo = invoiceResponse.CryptoInfo.First(o => o.GetpaymentMethodId() == paymentMethodId);
                model.InvoiceBitcoinUrl = WowneroPaymentType.Instance.GetPaymentLink(network,
                    new WowneroLikeOnChainPaymentMethodDetails() {DepositAddress = cryptoInfo.Address}, cryptoInfo.Due,
                    null);
                model.InvoiceBitcoinUrlQR = model.InvoiceBitcoinUrl;
            }
            else
            {
                model.InvoiceBitcoinUrl = "";
                model.InvoiceBitcoinUrlQR = "";
            }
        }
        public override string GetCryptoImage(PaymentMethodId paymentMethodId)
        {
            var network = _networkProvider.GetNetwork<WowneroLikeSpecificBtcPayNetwork>(paymentMethodId.CryptoCode);
            return GetCryptoImage(network);
        }

        public override string GetPaymentMethodName(PaymentMethodId paymentMethodId)
        {
            var network = _networkProvider.GetNetwork<WowneroLikeSpecificBtcPayNetwork>(paymentMethodId.CryptoCode);
            return GetPaymentMethodName(network);
        }
        public override IEnumerable<PaymentMethodId> GetSupportedPaymentMethods()
        {
            return _networkProvider.GetAll()
                .Where(network => network is WowneroLikeSpecificBtcPayNetwork)
                .Select(network => new PaymentMethodId(network.CryptoCode, PaymentType));
        }

        private string GetCryptoImage(WowneroLikeSpecificBtcPayNetwork network)
        {
            return network.CryptoImagePath;
        }


        private string GetPaymentMethodName(WowneroLikeSpecificBtcPayNetwork network)
        {
            return $"{network.DisplayName}";
        }
    }
}
#endif
