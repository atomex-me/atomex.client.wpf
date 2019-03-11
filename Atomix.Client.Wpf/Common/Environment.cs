using System.ComponentModel;

namespace Atomix.Client.Wpf.Common
{
    public class Env
    {
        public static bool IsInDesignerMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }
    }
}