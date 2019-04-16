using System;
using System.Windows;
using System.Windows.Input;
using Atomix.MarketData;
using Atomix.MarketData.Abstract;
using Atomix.Subsystems;
using Atomix.Subsystems.Abstract;
using Atomix.Wallet.Abstract;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Updates;

namespace Atomix.Client.Wpf.ViewModels
{
    public class MainViewModel : BaseViewModel, IMenuSelector
    {
        public IAtomixApp AtomixApp { get; set; }
        public IDialogViewer DialogViewer { get; set; }
        public LoginViewModel LoginViewModel { get; set; }
        public RegisterViewModel RegisterViewModel { get; set; }
        public PortfolioViewModel PortfolioViewModel { get; set; }
        public WalletsViewModel WalletsViewModel { get; set; }
        public ConversionViewModel ConversionViewModel { get; set; }
        public ExchangeViewModel ExchangeViewModel { get; set; }
        public SettingsViewModel SettingsViewModel { get; set; }

        private int _selectedMenuIndex;
        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set { _selectedMenuIndex = value; OnPropertyChanged(nameof(SelectedMenuIndex)); }
        }

        private string _installedVersion;
        public string InstalledVersion
        {
            get => _installedVersion;
            set { _installedVersion = value; OnPropertyChanged(nameof(InstalledVersion)); }
        }

        private bool _updatesReady;
        public bool UpdatesReady
        {
            get => _updatesReady;
            set { _updatesReady = value; OnPropertyChanged(nameof(UpdatesReady)); }
        }

        private bool _hasAccount;
        public bool HasAccount
        {
            get => _hasAccount;
            set { _hasAccount = value; OnPropertyChanged(nameof(HasAccount)); }
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(nameof(IsLocked)); }
        }

        //private bool _isAuthenticated;
        //public bool IsAuthenticated
        //{
        //    get => _isAuthenticated;
        //    set { _isAuthenticated = value; OnPropertyChanged(nameof(IsAuthenticated)); }
        //}

        //private bool _isConnected;
        //public bool IsConnected
        //{
        //    get => _isConnected;
        //    set { _isConnected = value; OnPropertyChanged(nameof(IsConnected)); }
        //}

        private bool _isExchangeConnected;
        public bool IsExchangeConnected
        {
            get => _isExchangeConnected;
            set { _isExchangeConnected = value; OnPropertyChanged(nameof(IsExchangeConnected)); }
        }

        private bool _isMarketDataConnected;
        public bool IsMarketDataConnected
        {
            get => _isMarketDataConnected;
            set { _isMarketDataConnected = value; OnPropertyChanged(nameof(IsMarketDataConnected)); }
        }

        private bool _isSwapConnected;
        public bool IsSwapConnected
        {
            get => _isSwapConnected;
            set { _isSwapConnected = value; OnPropertyChanged(nameof(IsSwapConnected)); }
        }

        private bool _isQuotesProviderAvailable;
        public bool IsQuotesProviderAvailable
        {
            get => _isQuotesProviderAvailable;
            set { _isQuotesProviderAvailable = value; OnPropertyChanged(nameof(IsQuotesProviderAvailable)); }
        }

        private string _login;
        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(nameof(Login)); }
        }

        public MainViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }
        public MainViewModel(
            IAtomixApp app,
            IDialogViewer dialogViewer)
        {
            AtomixApp = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            LoginViewModel = new LoginViewModel(DialogViewer) {RegisterViewModel = RegisterViewModel};
            RegisterViewModel = new RegisterViewModel(DialogViewer) {LoginViewModel = LoginViewModel};
            PortfolioViewModel = new PortfolioViewModel(AtomixApp);
            ConversionViewModel = new ConversionViewModel(AtomixApp, DialogViewer);
            WalletsViewModel = new WalletsViewModel(AtomixApp, DialogViewer, this, ConversionViewModel);
            ExchangeViewModel = new ExchangeViewModel(AtomixApp);
            SettingsViewModel = new SettingsViewModel();

            InstalledVersion = App.Updater.InstalledVersion.ToString();

            SubscribeToServices();
            SubscribeToUpdates(App.Updater);
        }

        public void SelectMenu(int index)
        {
            SelectedMenuIndex = index;
        }

        private void SubscribeToUpdates(Updater updater)
        {
            updater.UpdatesReady += OnUpdatesReadyEventHandler;
        }

        private void OnUpdatesReadyEventHandler(object sender, ReadyEventArgs e)
        {
            UpdatesReady = true;
        }

        private void SubscribeToServices()
        {
            AtomixApp.AccountChanged += OnAccountChangedEventHandler;

            AtomixApp.Terminal.ServiceConnected += OnTerminalServiceStateChangedEventHandler;
            AtomixApp.Terminal.ServiceDisconnected += OnTerminalServiceStateChangedEventHandler;

            AtomixApp.QuotesProvider.AvailabilityChanged += OnQuotesProviderAvailabilityChangedEventHandler;
        }

        private void OnAccountChangedEventHandler(object sender, AccountChangedEventArgs args)
        {
            if (args.OldAccount != null)
            {
                args.OldAccount.Locked -= OnAccountLockChangedEventHandler;
                args.OldAccount.Unlocked -= OnAccountLockChangedEventHandler;
            }

            var account = args.NewAccount;

            if (account == null) {
                HasAccount = false;
                return;
            }

            account.Locked += OnAccountLockChangedEventHandler;
            account.Unlocked += OnAccountLockChangedEventHandler;

            IsLocked = account.IsLocked;
            HasAccount = true;
        }

        private void OnTerminalServiceStateChangedEventHandler(object sender, TerminalServiceEventArgs args)
        {
            if (!(sender is ITerminal terminal))
                return;       

            IsExchangeConnected = terminal.IsServiceConnected(TerminalService.Exchange);
            IsMarketDataConnected = terminal.IsServiceConnected(TerminalService.MarketData);
            IsSwapConnected = terminal.IsServiceConnected(TerminalService.Swap);

            // subscribe to symbols updates
            if (args.Service == TerminalService.MarketData && IsMarketDataConnected)
            {
                terminal.SubscribeToMarketData(SubscriptionType.TopOfBook);
                terminal.SubscribeToMarketData(SubscriptionType.DepthTwenty);
            }
        }

        private void OnQuotesProviderAvailabilityChangedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            IsQuotesProviderAvailable = provider.IsAvailable;
        }

        private void OnAccountLockChangedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is IAccount account))
                return;

            IsLocked = account.IsLocked;
        }

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new Command(OnUpdateClick));

        private void OnUpdateClick()
        {
            Application.Current.Shutdown(101);
        }

        //private ICommand _lockCommand;
        //public ICommand LockCommand => _lockCommand ?? (_lockCommand = new Command(OnLockClick));

        //private async void OnLockClick()
        //{
        //    if (!AtomixApp.HasAccount)
        //        return;

        //    var account = AtomixApp.Account;

        //    if (account.IsLocked) {
        //        await UnlockAccountAsync(account);
        //    } else {
        //        account.Lock();
        //    }
        //}

        private ICommand _signOutCommand;
        public ICommand SignOutCommand => _signOutCommand ?? (_signOutCommand = new Command(SignOut));

        private void SignOut()
        {
            DialogViewer.HideAllDialogs();

            AtomixApp.Account.Lock();

            AtomixApp.UseAccount(
                account: null,
                restartTerminal: true);

            DialogViewer.ShowStartDialog(new StartViewModel(AtomixApp, DialogViewer));
        }

        //private Task UnlockAccountAsync(IAccount account)
        //{
        //    var viewModel = new UnlockViewModel(
        //        walletName: "wallet",
        //        unlockAction: account.Unlock);

        //    viewModel.Unlocked += (sender, args) => DialogViewer?.HideUnlockDialog();

        //    return DialogViewer?.ShowUnlockDialogAsync(viewModel);
        //}

        private void DesignerMode()
        {
            HasAccount = true;
        }

        //private ICommand _authCommand;
        //public ICommand AuthCommand => _authCommand ?? (_authCommand = new Command(OnAuthClick));

        //private void OnAuthClick()
        //{
        //    DialogViewer?.ShowLoginDialog(LoginViewModel);
        //}
    }
}