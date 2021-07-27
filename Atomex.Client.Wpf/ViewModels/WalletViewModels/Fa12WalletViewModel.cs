using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;

using Serilog;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Client.Wpf.ViewModels.SendViewModels;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public class Fa12WalletViewModel : BaseViewModel, IWalletViewModel
    {
        private const int ConversionViewIndex = 2;

        private ObservableCollection<TezosTokenTransferViewModel> _transactions;
        public ObservableCollection<TezosTokenTransferViewModel> Transactions
        {
            get => _transactions;
            set { _transactions = value; OnPropertyChanged(nameof(Transactions)); }
        }

        private CurrencyViewModel _currencyViewModel;
        public CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set { _currencyViewModel = value; OnPropertyChanged(nameof(CurrencyViewModel)); }
        }

        protected IAtomexApp App { get; }
        protected IDialogViewer DialogViewer { get; }
        private IMenuSelector MenuSelector { get; }
        private IConversionViewModel ConversionViewModel { get; }

        public string Header => CurrencyViewModel.Header;
        public Fa12Config Currency => CurrencyViewModel.Currency as Fa12Config;

        public Brush Background => IsSelected
            ? CurrencyViewModel.IconBrush
            : CurrencyViewModel.UnselectedIconBrush;

        public Brush OpacityMask => IsSelected
            ? CurrencyViewModel.IconBrush is ImageBrush ? null : CurrencyViewModel.IconMaskBrush
            : CurrencyViewModel.IconMaskBrush;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(OpacityMask));
            }
        }

        private bool _isBalanceUpdating;
        public bool IsBalanceUpdating
        {
            get => _isBalanceUpdating;
            set { _isBalanceUpdating = value; OnPropertyChanged(nameof(IsBalanceUpdating)); }
        }

        private CancellationTokenSource Cancellation { get; set; }

        public Fa12WalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public Fa12WalletViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel,
            CurrencyConfig currency)
        {
            App                 = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer        = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            MenuSelector        = menuSelector ?? throw new ArgumentNullException(nameof(menuSelector));
            ConversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);

            SubscribeToServices();

            // update transactions list
            _ = LoadTransactionsAsync();
        }

        private void SubscribeToServices()
        {
            App.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name == args.Currency)
                {
                    // update transactions list
                    await LoadTransactionsAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error.");
            }
        }

        protected async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}.", Currency.Name);

            try
            {
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .DataRepository
                    .GetTezosTokenTransfersAsync(Currency.TokenContractAddress)
                    .ConfigureAwait(false))
                    .ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions = new ObservableCollection<TezosTokenTransferViewModel>(
                        transactions.Select(t => new TezosTokenTransferViewModel(t, Currency))
                            .ToList()
                            .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
                },
                DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionsAsync error for {@currency}.", Currency?.Name);
            }
        }

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ??= new Command(OnSendClick);

        private ICommand _receiveCommand;
        public ICommand ReceiveCommand => _receiveCommand ??= new Command(OnReceiveClick);

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ??= new Command(OnConvertClick);

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= new Command(OnUpdateClick);

        private ICommand _addressesCommand;
        public ICommand AddressesCommand => _addressesCommand ??= new Command(OnAddressesClick);

        private ICommand _cancelUpdateCommand;
        public ICommand CancelUpdateCommand => _cancelUpdateCommand ??= new Command(() =>
        {
            Cancellation.Cancel();
        });

        private void OnSendClick()
        {
            var sendViewModel = SendViewModelCreator.CreateViewModel(App, DialogViewer, Currency);
            var sendPageId = SendViewModelCreator.GetSendPageId(Currency);

            DialogViewer.ShowDialog(Dialogs.Send, sendViewModel, defaultPageId: sendPageId);
        }

        private void OnReceiveClick()
        {
            var tezosConfig = App.Account.Currencies.GetByName(TezosConfig.Xtz);
            var receiveViewModel = new ReceiveViewModel(App, tezosConfig, Currency.TokenContractAddress);

            DialogViewer.ShowDialog(Dialogs.Receive, receiveViewModel);
        }

        private void OnConvertClick()
        {
            MenuSelector.SelectMenu(ConversionViewIndex);
            ConversionViewModel.FromCurrency = Currency;
        }

        protected async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;

            IsBalanceUpdating = true;

            Cancellation = new CancellationTokenSource();

            try
            {
                await App.Account
                    .GetCurrencyAccount<Fa12Account>(Currency.Name)
                    .UpdateBalanceAsync(Cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "Fa12WalletViewModel.OnUpdateClick error.");
                // todo: message to user!?
            }

            IsBalanceUpdating = false;
        }

        private void OnAddressesClick()
        {
            DialogViewer.ShowDialog(
                dialogId: Dialogs.Addresses,
                dataContext: new AddressesViewModel(App, DialogViewer, Currency));
        }

        protected virtual void DesignerMode()
        {
        }
    }
}