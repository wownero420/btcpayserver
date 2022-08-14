using Newtonsoft.Json;

namespace BTCPayServer.Services.Altcoins.Wownero.RPC.Models
{
    public partial class GetAccountsRequest
    {
        [JsonProperty("tag")] public string Tag { get; set; }
    }
}
