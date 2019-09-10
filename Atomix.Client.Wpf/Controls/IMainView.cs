using System;
using System.ComponentModel;

namespace Atomix.Client.Wpf.Controls
{
    public interface IMainView
    {
        event CancelEventHandler MainViewClosing;
        event EventHandler Inactivity;

        void Close();
        void StartInactivityControl(TimeSpan timeOut);
        void StopInactivityControl();
    }
}
