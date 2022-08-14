#if ALTCOINS
using System;
using System.Collections.Generic;

namespace BTCPayServer.Services.Altcoins.Wownero.Configuration
{
    public class WowneroLikeConfiguration
    {
        public Dictionary<string, WowneroLikeConfigurationItem> WowneroLikeConfigurationItems { get; set; } =
            new Dictionary<string, WowneroLikeConfigurationItem>();
    }

    public class WowneroLikeConfigurationItem
    {
        public Uri DaemonRpcUri { get; set; }
        public Uri InternalWalletRpcUri { get; set; }
        public string WalletDirectory { get; set; }
    }
}
#endif
