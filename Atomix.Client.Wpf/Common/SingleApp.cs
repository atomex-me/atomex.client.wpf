using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Atomix.Client.Wpf.Common
{
    public static class SingleApp
    {
        [DllImport("user32.dll")]
        private static extern void SwitchToThisWindow(IntPtr hWnd, bool turnon);

        private static Mutex AppMutex;

        public static bool TryStart(string name)
        {
            AppMutex = new Mutex(false, name, out var isSingle);
            return isSingle;
        }

        public static void Close()
        {
            if (AppMutex?.SafeWaitHandle?.IsClosed == false)
                AppMutex.Close();
        }

        public static void CloseAndSwitch()
        {
            if (AppMutex?.SafeWaitHandle?.IsClosed == false)
                AppMutex.Close();

            var current = Process.GetCurrentProcess();
            var target = Process.GetProcessesByName(current.ProcessName).FirstOrDefault(x => x.Id != current.Id);
            if (target != null)
                SwitchToThisWindow(target.MainWindowHandle, true);
        }
    }
}
