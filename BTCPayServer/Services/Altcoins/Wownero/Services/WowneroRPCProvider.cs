#if ALTCOINS
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading.Tasks;
using BTCPayServer.Services.Altcoins.Wownero.Configuration;
using BTCPayServer.Services.Altcoins.Wownero.RPC;
using BTCPayServer.Services.Altcoins.Wownero.RPC.Models;
using NBitcoin;

namespace BTCPayServer.Services.Altcoins.Wownero.Services
{
    public class WowneroRPCProvider
    {
        private readonly WowneroLikeConfiguration _wowneroLikeConfiguration;
        private readonly EventAggregator _eventAggregator;
        public ImmutableDictionary<string, JsonRpcClient> DaemonRpcClients;
        public ImmutableDictionary<string, JsonRpcClient> WalletRpcClients;

        private readonly ConcurrentDictionary<string, WowneroLikeSummary> _summaries =
            new ConcurrentDictionary<string, WowneroLikeSummary>();

        public ConcurrentDictionary<string, WowneroLikeSummary> Summaries => _summaries;

        public WowneroRPCProvider(WowneroLikeConfiguration wowneroLikeConfiguration, EventAggregator eventAggregator, IHttpClientFactory httpClientFactory)
        {
            _wowneroLikeConfiguration = wowneroLikeConfiguration;
            _eventAggregator = eventAggregator;
            DaemonRpcClients =
                _wowneroLikeConfiguration.WowneroLikeConfigurationItems.ToImmutableDictionary(pair => pair.Key,
                    pair => new JsonRpcClient(pair.Value.DaemonRpcUri, "", "", httpClientFactory.CreateClient()));
            WalletRpcClients =
                _wowneroLikeConfiguration.WowneroLikeConfigurationItems.ToImmutableDictionary(pair => pair.Key,
                    pair => new JsonRpcClient(pair.Value.InternalWalletRpcUri, "", "", httpClientFactory.CreateClient()));
        }

        public bool IsAvailable(string cryptoCode)
        {
            cryptoCode = cryptoCode.ToUpperInvariant();
            return _summaries.ContainsKey(cryptoCode) && IsAvailable(_summaries[cryptoCode]);
        }

        private bool IsAvailable(WowneroLikeSummary summary)
        {
            return summary.Synced &&
                   summary.WalletAvailable;
        }

        public async Task<WowneroLikeSummary> UpdateSummary(string cryptoCode)
        {
            if (!DaemonRpcClients.TryGetValue(cryptoCode.ToUpperInvariant(), out var daemonRpcClient) ||
                !WalletRpcClients.TryGetValue(cryptoCode.ToUpperInvariant(), out var walletRpcClient))
            {
                return null;
            }

            var summary = new WowneroLikeSummary();
            try
            {
                var daemonResult =
                    await daemonRpcClient.SendCommandAsync<JsonRpcClient.NoRequestModel, SyncInfoResponse>("sync_info",
                        JsonRpcClient.NoRequestModel.Instance);
                summary.TargetHeight = daemonResult.TargetHeight.GetValueOrDefault(0);
                summary.CurrentHeight = daemonResult.Height;
                summary.TargetHeight = summary.TargetHeight == 0 ? summary.CurrentHeight : summary.TargetHeight;
                summary.Synced = daemonResult.Height >= summary.TargetHeight && summary.CurrentHeight > 0;
                summary.UpdatedAt = DateTime.UtcNow;
                summary.DaemonAvailable = true;
            }
            catch
            {
                summary.DaemonAvailable = false;
            }

            try
            {
                var walletResult =
                    await walletRpcClient.SendCommandAsync<JsonRpcClient.NoRequestModel, GetHeightResponse>(
                        "get_height", JsonRpcClient.NoRequestModel.Instance);

                summary.WalletHeight = walletResult.Height;
                summary.WalletAvailable = true;
            }
            catch
            {
                summary.WalletAvailable = false;
            }

            var changed = !_summaries.ContainsKey(cryptoCode) || IsAvailable(cryptoCode) != IsAvailable(summary);

            _summaries.AddOrReplace(cryptoCode, summary);
            if (changed)
            {
                _eventAggregator.Publish(new WowneroDaemonStateChange() { Summary = summary, CryptoCode = cryptoCode });
            }

            return summary;
        }


        public class WowneroDaemonStateChange
        {
            public string CryptoCode { get; set; }
            public WowneroLikeSummary Summary { get; set; }
        }

        public class WowneroLikeSummary
        {
            public bool Synced { get; set; }
            public long CurrentHeight { get; set; }
            public long WalletHeight { get; set; }
            public long TargetHeight { get; set; }
            public DateTime UpdatedAt { get; set; }
            public bool DaemonAvailable { get; set; }
            public bool WalletAvailable { get; set; }
        }
    }
}
#endif
