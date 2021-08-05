using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using NBitcoin;
using Serilog;
using Network = NBitcoin.Network;

using Atomex.Blockchain;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Client.Wpf.ViewModels.SendViewModels;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public class WalletViewModel : BaseViewModel, IWalletViewModel
    {
        public const int ConversionViewIndex = 2;

        private ObservableCollection<TransactionViewModel> _transactions;
        public ObservableCollection<TransactionViewModel> Transactions
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
        public CurrencyConfig Currency => CurrencyViewModel.Currency;

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

        public WalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public WalletViewModel(
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
            CurrencyViewModel   = CurrencyViewModelCreator.CreateViewModel(currency);

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
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        protected async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}", Currency.Name);

            try
            {
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetTransactionsAsync(Currency.Name))
                    .ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t => TransactionViewModelCreator
                            .CreateViewModel(t, Currency))
                            .ToList()
                            .SortList((t1, t2) => t2.Time.CompareTo(t1.Time))
                            .ForEachDo(t =>
                            {
                                t.UpdateClicked += UpdateTransactonEventHandler;
                                t.RemoveClicked += RemoveTransactonEventHandler;
                            }));
                },
                DispatcherPriority.Background);
            }
            catch (OperationCanceledException)
            {
                Log.Debug("LoadTransactionsAsync canceled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency?.Name);
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
            var receiveViewModel = new ReceiveViewModel(App, Currency);

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
                var scanner = new HdWalletScanner(App.Account);

                await scanner.ScanAsync(
                    currency: Currency.Name,
                    skipUsed: true,
                    cancellationToken: Cancellation.Token);

                await LoadTransactionsAsync();
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "WalletViewModel.OnUpdateClick");
                // todo: message to user!?
            }

            IsBalanceUpdating = false;
        }

        private void OnAddressesClick()
        {
            DialogViewer.ShowDialog(Dialogs.Addresses, new AddressesViewModel(App, DialogViewer, Currency));
        }

        private void UpdateTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            // todo:
        }

        private async void RemoveTransactonEventHandler(object sender, TransactionEventArgs args)
        {
            if (App.Account == null)
                return;

            try
            {
                var txId = $"{args.Transaction.Id}:{args.Transaction.Currency}";

                var isRemoved = await App.Account
                    .RemoveTransactionAsync(txId);

                if (isRemoved)
                    await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }
        }

        protected virtual void DesignerMode()
        {
            var currencies = DesignTime.Currencies.ToList();

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[3], subscribeToUpdates: false);
            CurrencyViewModel.TotalAmount             = 0.01012345m;
            CurrencyViewModel.TotalAmountInBase       = 16.51m;
            CurrencyViewModel.AvailableAmount         = 0.01010005m;
            CurrencyViewModel.AvailableAmountInBase   = 16.00m;
            CurrencyViewModel.UnconfirmedAmount       = 0.00002m;
            CurrencyViewModel.UnconfirmedAmountInBase = 0.5m;

            var transactions = new List<TransactionViewModel>
            {
                new BitcoinBasedTransactionViewModel(new BitcoinBasedTransaction("BTC", Transaction.Create(Network.TestNet)), DesignTime.Currencies.Get<BitcoinConfig>("BTC"))
                {
                    Description  = "Sent 0.00124 BTC",
                    Amount       = -0.00124m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time         = DateTime.Now,
                },
                new BitcoinBasedTransactionViewModel(new BitcoinBasedTransaction("BTC", Transaction.Create(Network.TestNet)), DesignTime.Currencies.Get<BitcoinConfig>("BTC"))
                {
                    Description  = "Received 1.00666 BTC",
                    Amount       = 1.00666m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time         = DateTime.Now,
                }
            };

            Transactions = new ObservableCollection<TransactionViewModel>(
                transactions.SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
        }
    }
}