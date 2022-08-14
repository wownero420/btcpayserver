using Newtonsoft.Json;

namespace BTCPayServer.Services.Altcoins.Wownero.RPC.Models
{
    public partial class Peer
    {
        [JsonProperty("info")] public Info Info { get; set; }
    }
}
