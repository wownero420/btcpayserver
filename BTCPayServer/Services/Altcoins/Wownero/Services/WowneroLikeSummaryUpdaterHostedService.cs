#if ALTCOINS
using System;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Logging;
using BTCPayServer.Services.Altcoins.Wownero.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Services.Altcoins.Wownero.Services
{
    public class WowneroLikeSummaryUpdaterHostedService : IHostedService
    {
        private readonly WowneroRPCProvider _WowneroRpcProvider;
        private readonly WowneroLikeConfiguration _wowneroLikeConfiguration;

        public Logs Logs { get; }

        private CancellationTokenSource _Cts;
        public WowneroLikeSummaryUpdaterHostedService(WowneroRPCProvider wowneroRpcProvider, WowneroLikeConfiguration wowneroLikeConfiguration, Logs logs)
        {
            _WowneroRpcProvider = wowneroRpcProvider;
            _wowneroLikeConfiguration = wowneroLikeConfiguration;
            Logs = logs;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            foreach (var wowneroLikeConfigurationItem in _wowneroLikeConfiguration.WowneroLikeConfigurationItems)
            {
                _ = StartLoop(_Cts.Token, wowneroLikeConfigurationItem.Key);
            }
            return Task.CompletedTask;
        }

        private async Task StartLoop(CancellationToken cancellation, string cryptoCode)
        {
            Logs.PayServer.LogInformation($"Starting listening Wownero-like daemons ({cryptoCode})");
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await _WowneroRpcProvider.UpdateSummary(cryptoCode);
                        if (_WowneroRpcProvider.IsAvailable(cryptoCode))
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1), cancellation);
                        }
                        else
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), cancellation);
                        }
                    }
                    catch (Exception ex) when (!cancellation.IsCancellationRequested)
                    {
                        Logs.PayServer.LogError(ex, $"Unhandled exception in Summary updater ({cryptoCode})");
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellation);
                    }
                }
            }
            catch when (cancellation.IsCancellationRequested) { }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Cts?.Cancel();
            return Task.CompletedTask;
        }
    }
}
#endif
