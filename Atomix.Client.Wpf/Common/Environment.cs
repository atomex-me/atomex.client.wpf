using System.ComponentModel;

namespace Atomix.Client.Wpf.Common
{
    public static class Env
    {
        public static bool IsInDesignerMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }
    }
}