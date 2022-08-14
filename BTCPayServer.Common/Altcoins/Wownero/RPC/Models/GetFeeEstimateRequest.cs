using Newtonsoft.Json;

namespace BTCPayServer.Services.Altcoins.Wownero.RPC.Models
{
    public class GetFeeEstimateRequest
    {
        [JsonProperty("grace_blocks")] public int? GraceBlocks { get; set; }
    }
}
