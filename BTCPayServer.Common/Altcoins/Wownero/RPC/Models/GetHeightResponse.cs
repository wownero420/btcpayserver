using Newtonsoft.Json;

namespace BTCPayServer.Services.Altcoins.Wownero.RPC.Models
{
    public partial class GetHeightResponse
    {
        [JsonProperty("height")] public long Height { get; set; }
    }
}
