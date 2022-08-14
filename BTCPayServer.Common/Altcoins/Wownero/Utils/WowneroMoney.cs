using System.Globalization;

namespace BTCPayServer.Services.Altcoins.Wownero.Utils
{
    public class WowneroMoney
    {
        public static decimal Convert(long piconero)
        {
            var amt = piconero.ToString(CultureInfo.InvariantCulture).PadLeft(12, '0');
            amt = amt.Length == 12 ? $"0.{amt}" : amt.Insert(amt.Length - 12, ".");

            return decimal.Parse(amt, CultureInfo.InvariantCulture);
        }

        public static long Convert(decimal wownero)
        {
            return System.Convert.ToInt64(wownero * 1000000000000);
        }
    }
}
