using System.Globalization;

namespace Atomix.Client.Wpf.Common
{
    public static class DecimalExtensions
    {
        public static decimal TruncateByFormat(this decimal d, string format)
        {
            var s = d.ToString(format, CultureInfo.InvariantCulture);

            return decimal.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}