#if ALTCOINS
using System.Collections.Generic;
using System.Linq;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Client.Models;

namespace BTCPayServer.Services.Altcoins.Wownero.Services
{
    public class WowneroSyncSummaryProvider : ISyncSummaryProvider
    {
        private readonly WowneroRPCProvider _wowneroRpcProvider;

        public WowneroSyncSummaryProvider(WowneroRPCProvider wowneroRpcProvider)
        {
            _wowneroRpcProvider = wowneroRpcProvider;
        }

        public bool AllAvailable()
        {
            return _wowneroRpcProvider.Summaries.All(pair => pair.Value.WalletAvailable);
        }

        public string Partial { get; } = "Wownero/WowneroSyncSummary";
        public IEnumerable<ISyncStatus> GetStatuses()
        {
            return _wowneroRpcProvider.Summaries.Select(pair => new WowneroSyncStatus()
            {
                Summary = pair.Value, CryptoCode = pair.Key
            });
        }
    }

    public class WowneroSyncStatus: SyncStatus, ISyncStatus
    {
        public override bool Available
        {
            get
            {
                return Summary?.WalletAvailable ?? false;
            }
        }

        public WowneroRPCProvider.WowneroLikeSummary Summary { get; set; }
    }
}
#endif
