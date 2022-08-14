namespace BTCPayServer
{
    public class WowneroLikeSpecificBtcPayNetwork : BTCPayNetworkBase
    {
        public int MaxTrackedConfirmation = 10;
        public string UriScheme { get; set; }
    }
}
