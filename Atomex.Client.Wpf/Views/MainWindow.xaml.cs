using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Client.Wpf.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Atomex.Client.Wpf.Views
{
    public partial class MainWindow : IDialogViewer, IMainView
    {
        private ChildWindow _startView;
        private ChildWindow _createWalletView;
        private ChildWindow _sendView;
        private ChildWindow _conversionConfirmationView;
        private ChildWindow _receiveView;
        private ChildWindow _unlockView;
        private ChildWindow _myWalletsView;

        private DispatcherTimer _activityTimer;
        private bool _inactivityControlEnabled;
        private Point _inactiveMousePosition = new Point(0, 0);

        public event CancelEventHandler MainViewClosing;
        public event EventHandler Inactivity;

        public MainWindow()
        {
            InitializeComponent();

            Closing += (sender, args) => MainViewClosing?.Invoke(sender, args);

            InputManager.Current.PreProcessInput += (sender, args) =>
            {
                var inputEventArgs = args.StagingItem.Input;

                if (inputEventArgs is MouseEventArgs || inputEventArgs is KeyboardEventArgs)
                {
                    if (args.StagingItem.Input is MouseEventArgs mouseEventArgs)
                    {
                        // no button is pressed and the position is still the same as the application became inactive
                        if (mouseEventArgs.LeftButton == MouseButtonState.Released &&
                            mouseEventArgs.RightButton == MouseButtonState.Released &&
                            mouseEventArgs.MiddleButton == MouseButtonState.Released &&
                            mouseEventArgs.XButton1 == MouseButtonState.Released &&
                            mouseEventArgs.XButton2 == MouseButtonState.Released &&
                            _inactiveMousePosition == mouseEventArgs.GetPosition(MainDockerPanel))
                            return;
                    }

                    if (_inactivityControlEnabled && _activityTimer != null)
                    {
                        _activityTimer.Stop();
                        _activityTimer.Start();
                    }
                }
            };
        }

        public void StartInactivityControl(TimeSpan timeOut)
        {
            _activityTimer = new DispatcherTimer { Interval = timeOut, IsEnabled = true };
            _activityTimer.Tick += (sender, args) =>
            {
                _inactiveMousePosition = Mouse.GetPosition(MainDockerPanel);

                Inactivity?.Invoke(sender, args);
            };

            _inactivityControlEnabled = true;
        }

        public void StopInactivityControl()
        {
            _inactivityControlEnabled = false;
            _activityTimer?.Stop();
        }

        public void HideAllDialogs()
        {
            this.CloseAllChildWindows();
        }

        public void ShowStartDialog(object dataContext)
        {
            ShowDialogAsync<StartView>(dataContext, ref _startView);
        }

        public void HideStartDialog()
        {
            _startView.Close();
        }

        public void ShowCreateWalletDialog(object dataContext)
        {
            ShowDialogAsync<CreateWalletView>(dataContext, ref _createWalletView);
        }

        public void HideCreateWalletDialog()
        {
            _createWalletView.Close();
        }

        public void ShowSendDialog(object dataContext, Action dialogLoaded = null)
        {
            if (_sendView != null && _sendView.IsOpen)
                return;

            _sendView = new FrameView {DataContext = dataContext};

            if (dialogLoaded != null)
                _sendView.Loaded += (sender, args) => { dialogLoaded(); };

            this.ShowChildWindowAsync(
                dialog: _sendView,
                overlayFillBehavior: ChildWindowManager.OverlayFillBehavior.FullWindow);
        }

        public void HideSendDialog()
        {
            _sendView.Close();
        }

        public void ShowConversionConfirmationDialog(object dataContext, Action dialogLoaded = null)
        {
            if (_conversionConfirmationView != null && _conversionConfirmationView.IsOpen)
                return;

            _conversionConfirmationView = new FrameView { DataContext = dataContext };

            if (dialogLoaded != null)
                _conversionConfirmationView.Loaded += (sender, args) => { dialogLoaded(); };

            this.ShowChildWindowAsync(_conversionConfirmationView);
        }

        public void HideConversionConfirmationDialog()
        {
            _conversionConfirmationView.Close();
        }

        public void ShowReceiveDialog(object dataContext)
        {
            if (_receiveView != null) {
                if (_receiveView.IsOpen)
                    return;

                _receiveView.DataContext = dataContext;
            } else {
                _receiveView = new ReceiveView {DataContext = dataContext};
            }

            this.ShowChildWindowAsync(
                dialog: _receiveView,
                overlayFillBehavior: ChildWindowManager.OverlayFillBehavior.FullWindow);
        }

        public void HideReceiveDialog()
        {
            _receiveView.Close();
        }

        public void ShowUnlockDialog(object dataContext, EventHandler canceled = null)
        {
            ShowDialogAsync<UnlockView>(
                dataContext: dataContext,
                childView: ref _unlockView,
                canceled: canceled);
        }

        public void HideUnlockDialog()
        {
            _unlockView?.Close();
        }

        public void ShowMyWalletsDialog(object dataContext)
        {
            ShowDialogAsync<MyWalletsView>(dataContext, ref _myWalletsView);
        }

        public void HideMyWalletsDialog()
        {
            _myWalletsView?.Close();
        }

        public void ShowMessage(string title, string message)
        {
            this.ShowMessageAsync(title, message);
        }

        public Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style)
        {
            return this.ShowMessageAsync(title, message, style, settings: null);
        }

        private void ShowDialogAsync<TView>(
            object dataContext,
            ref ChildWindow childView,
            EventHandler canceled = null)
            where TView : ChildWindow, new()
        {
            if (childView != null && childView.IsOpen)
                return;

            childView = new TView {DataContext = dataContext};
            if (canceled != null)
                childView.CloseButtonClicked += canceled;

            this.ShowChildWindowAsync(
                dialog: childView,
                overlayFillBehavior: ChildWindowManager.OverlayFillBehavior.FullWindow);
        }
    }
}