using System;
using System.Threading.Tasks;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace Atomix.Client.Wpf.Views
{
    public partial class MainWindow : IDialogViewer
    {
        private ChildView _loginView;
        private ChildView _registerView;
        private ChildView _startView;
        private ChildView _createWalletView;
        private ChildView _sendView;
        private ChildView _conversionConfirmationView;
        private ChildView _receiveView;
        private ChildView _unlockView;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void HideAllDialogs()
        {
            this.CloseAllChildViews();

            HideOverlay();
        }

        public void ShowLoginDialog(object dataContext)
        {
            if (_loginView != null && _loginView.IsOpen)
                return;

            this.ShowChildViewAsync(_loginView = new LoginView {DataContext = dataContext});
        }

        public void HideLoginDialog(bool hideOverlay = true)
        {
            CloseChildView(_loginView, hideOverlay);
        }

        public void ShowRegisterDialog(object dataContext)
        {
            if (_registerView != null && _registerView.IsOpen)
                return;

            this.ShowChildViewAsync(_registerView = new RegisterView {DataContext = dataContext});
        }

        public void HideRegisterDialog(bool hideOverlay = true)
        {
            CloseChildView(_registerView, hideOverlay);
        }

        public void ShowStartDialog(object dataContext)
        {
            if (_startView != null && _startView.IsOpen)
                return;

            this.ShowChildViewAsync(_startView = new StartView {DataContext = dataContext});
        }

        public void HideStartDialog(bool hideOverlay = true)
        {
            CloseChildView(_startView, hideOverlay);
        }

        public void ShowCreateWalletDialog(object dataContext)
        {
            if (_createWalletView != null && _createWalletView.IsOpen)
                return;

            this.ShowChildViewAsync(_createWalletView = new CreateWalletView {DataContext = dataContext});
        }

        public void HideCreateWalletDialog(bool hideOverlay = true)
        {
            CloseChildView(_createWalletView, hideOverlay);
        }

        public void ShowSendDialog(object dataContext, Action dialogLoaded = null)
        {
            if (_sendView != null && _sendView.IsOpen)
                return;

            _sendView = new FrameView {DataContext = dataContext};

            if (dialogLoaded != null)
                _sendView.Loaded += (sender, args) => { dialogLoaded(); };

            this.ShowChildViewAsync(_sendView);
        }

        public void HideSendDialog(bool hideOverlay = true)
        {
            CloseChildView(_sendView, hideOverlay);
        }

        public void ShowConversionConfirmationDialog(object dataContext, Action dialogLoaded = null)
        {
            if (_conversionConfirmationView != null && _conversionConfirmationView.IsOpen)
                return;

            _conversionConfirmationView = new FrameView { DataContext = dataContext };

            if (dialogLoaded != null)
                _conversionConfirmationView.Loaded += (sender, args) => { dialogLoaded(); };

            this.ShowChildViewAsync(_conversionConfirmationView);
        }

        public void HideConversionConfirmationDialog(bool hideOverlay = true)
        {
            CloseChildView(_conversionConfirmationView, hideOverlay);
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

            this.ShowChildViewAsync(_receiveView);
        }

        public void HideReceiveDialog(bool hideOverlay = true)
        {
            CloseChildView(_receiveView, hideOverlay);
        }

        private void CloseChildView(ChildView childView, bool hideOverlay)
        {
            if (childView != null) {
                childView.HideOverlayWhenClose = hideOverlay;
                childView.Close();
            }
        }

        public Task ShowUnlockDialogAsync(object dataContext)
        {
            if (_unlockView != null && _unlockView.IsOpen)
                return Task.CompletedTask;
            
            return this.ShowChildViewAsync(_unlockView = new UnlockView
            {
                HideOverlayWhenClose = !IsOverlayVisible(),
                DataContext = dataContext
            });
        }

        public void HideUnlockDialog()
        {
            _unlockView?.Close();
        }

        public void ShowMessage(string title, string message)
        {
            this.ShowMessageAsync(title, message);
        }
    }
}