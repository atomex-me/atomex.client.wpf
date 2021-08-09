using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using MahApps.Metro.Controls.Dialogs;

using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels;
using Atomex.Client.Wpf.Views.SendViews;

namespace Atomex.Client.Wpf.Views
{
    public delegate Page PageConstructor();

    public delegate ChildWindow DialogConstructor(
        int dialogId,
        object dataContext,
        Action loaded,
        Action canceled,
        int pageId);

    public partial class MainWindow : IDialogViewer, IMainView
    {
        private IDictionary<int, ChildWindow> _dialogs;
        private IDictionary<int, DialogConstructor> _dialogsFactory;
        private IDictionary<int, PageConstructor> _pagesFactory;

        private DispatcherTimer _activityTimer;
        private bool _inactivityControlEnabled;
        private Point _inactiveMousePosition = new Point(0, 0);

        public event CancelEventHandler MainViewClosing;
        public event EventHandler Inactivity;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDialogs();

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

        private void InitializeDialogs()
        {
            _dialogs = new Dictionary<int, ChildWindow>();

            _dialogsFactory = new Dictionary<int, DialogConstructor>
            {
                { Dialogs.Start,        ShowDialogAsync<StartView> },
                { Dialogs.CreateWallet, ShowDialogAsync<CreateWalletView> },
                { Dialogs.MyWallets,    ShowDialogAsync<MyWalletsView> },
                { Dialogs.Receive,      ShowDialogAsync<ReceiveView> },
                { Dialogs.Unlock,       ShowDialogAsync<UnlockView> },
                { Dialogs.Send,         ShowDialogAsync<FrameView> },
                { Dialogs.Delegate,     ShowDialogAsync<FrameView> },
                { Dialogs.Convert,      ShowDialogAsync<FrameView> },
                { Dialogs.Addresses,    ShowDialogAsync<AddressesView> }
            };

            _pagesFactory = new Dictionary<int, PageConstructor>
            {
                { Pages.Message,                () => new MessagePage() },
                { Pages.SendBitcoinBased,       () => new BitcoinBasedSendPage() },
                { Pages.SendEthereum,           () => new EthereumSendPage() },
                { Pages.SendTezos,              () => new SendPage() },
                { Pages.SendErc20,              () => new EthereumSendPage() },
                { Pages.SendFa12,               () => new SendPage() },
                { Pages.SendTezosTokens,        () => new TezosTokensSendPage() },
                { Pages.SendConfirmation,       () => new SendConfirmationPage() },
                { Pages.Sending,                () => new SendingPage() },
                { Pages.Delegate,               () => new DelegatePage() },
                { Pages.DelegateConfirmation,   () => new DelegateConfirmationPage() },
                { Pages.Delegating,             () => new DelegatingPage() },
                { Pages.ConversionConfirmation, () => new ConversionConfirmationPage() }
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

        public void ShowDialog(
            int dialogId,
            object dataContext,
            Action loaded = null,
            Action canceled = null,
            int defaultPageId = 0)
        {
            if (_dialogs.TryGetValue(dialogId, out var childView) && childView != null && childView.IsOpen)
                return;

            if (!_dialogsFactory.TryGetValue(dialogId, out var dialogConstructor))
                throw new ArgumentException($"Dialog constructor for dialog {dialogId} not found");

            childView = dialogConstructor(dialogId, dataContext, loaded, canceled, defaultPageId);

            _dialogs[dialogId] = childView;
        }

        public void HideDialog(int dialogId)
        {
            if (!_dialogs.TryGetValue(dialogId, out var childView))
                return;

            _dialogs.Remove(dialogId);

            childView.Close();
        }

        public void HideAllDialogs()
        {
            _dialogs.Clear();

            this.CloseAllChildWindows();
        }

        public void PushPage(int dialogId, int pageId, object dataContext = null, Action onClose = null)
        {
            if (!_dialogs.TryGetValue(dialogId, out var childView))
                throw new ArgumentException($"Dialog {dialogId} not found");

            if (!(childView is FrameView frameView))
                throw new Exception("Invalid dialog type");

            if (childView != null && onClose != null)
                childView.CloseButtonClicked += (s, e) => onClose();

            if (!_pagesFactory.TryGetValue(pageId, out var pageConstructor))
                throw new ArgumentException($"Page constructor for page {pageId} not found");

            var page = pageConstructor();
            page.DataContext = dataContext;

            frameView.NavigationService.Navigate(page, dataContext);
        }

        public void PopPage(int dialogId)
        {
            if (!_dialogs.TryGetValue(dialogId, out var childView))
                throw new ArgumentException($"Dialog {dialogId} not found");

            if (!(childView is FrameView frameView))
                throw new Exception("Invalid dialog type");

            if (frameView.NavigationService.CanGoBack)
                frameView.NavigationService.GoBack();
        }

        public void Back(int dialogId) =>
            PopPage(dialogId);

        public void ShowMessage(string title, string message) =>
            this.ShowMessageAsync(title, message);

        public Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style) =>
            this.ShowMessageAsync(title, message, style, settings: null);

        private ChildWindow ShowDialogAsync<TView>(
            int dialogId,
            object dataContext,
            Action loaded = null,
            Action canceled = null,
            int defaultPageId = 0)
            where TView : ChildWindow, new()
        {
            var childView = new TView {DataContext = dataContext};

            if (defaultPageId != 0)
                childView.Loaded += (s, e) => PushPage(dialogId, defaultPageId, dataContext);

            if (loaded != null)
                childView.Loaded += (s, e) => loaded();

            if (canceled != null)
                childView.CloseButtonClicked += (s, e) => canceled();

            this.ShowChildWindowAsync(
                dialog: childView,
                overlayFillBehavior: ChildWindowManager.OverlayFillBehavior.FullWindow);

            return childView;
        }

        private ProgressDialogController _progressController;

        public async Task ShowProgressAsync(
            string title,
            string message,
            Action canceled = null,
            CancellationToken cancellationToken = default)
        {
            _progressController = await this.ShowProgressAsync(
                title: title,
                message: message,
                isCancelable: true,
                settings: new MetroDialogSettings { CancellationToken = cancellationToken });

            _progressController.Canceled += (o, e) =>
            {
                canceled?.Invoke();
            };
        }

        public void HideProgress()
        {
            if (_progressController == null)
                return;

            _ = _progressController.CloseAsync();
        }
    }
}