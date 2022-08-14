#if ALTCOINS
using BTCPayServer.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Services.Altcoins.Wownero.RPC
{
    [Route("[controller]")]
    [OnlyIfSupportAttribute("WOW")]
    public class WowneroLikeDaemonCallbackController : Controller
    {
        private readonly EventAggregator _eventAggregator;

        public WowneroLikeDaemonCallbackController(EventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }
        [HttpGet("block")]
        public IActionResult OnBlockNotify(string hash, string cryptoCode)
        {
            _eventAggregator.Publish(new WowneroEvent()
            {
                BlockHash = hash,
                CryptoCode = cryptoCode.ToUpperInvariant()
            });
            return Ok();
        }
        [HttpGet("tx")]
        public IActionResult OnTransactionNotify(string hash, string cryptoCode)
        {
            _eventAggregator.Publish(new WowneroEvent()
            {
                TransactionHash = hash,
                CryptoCode = cryptoCode.ToUpperInvariant()
            });
            return Ok();
        }

    }
}
#endif
