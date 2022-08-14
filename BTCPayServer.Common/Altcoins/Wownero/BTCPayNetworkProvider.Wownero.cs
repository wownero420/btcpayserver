using NBitcoin;

namespace BTCPayServer
{
    public partial class BTCPayNetworkProvider
    {
        public void InitWownero()
        {
            Add(new WowneroLikeSpecificBtcPayNetwork()
            {
                CryptoCode = "WOW",
                DisplayName = "Wownero",
                Divisibility = 12,
                BlockExplorerLink =
                    NetworkType == ChainName.Mainnet
                        ? "https://explore.wownero.com/tx/{0}"
                        : "https://explore.wownero.com/tx/{0}",
                DefaultRateRules = new[]
                {
                    "WOW_X = WOW_BTC * BTC_X",
                    "WOW_BTC = coingecko(WOW_BTC)"
                },
                CryptoImagePath = "/imlegacy/wownero.svg",
                UriScheme = "wownero"
            });
        }
    }
}
