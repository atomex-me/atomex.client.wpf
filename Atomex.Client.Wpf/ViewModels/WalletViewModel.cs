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
using Atomex.Blockchain;
using Atomex.Common;
using Atomex.Core.Entities;
using Atomex.Wallet;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.SendViewModels;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class WalletViewModel : BaseViewModel
    {
        private const int ConversionViewIndex = 2;

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

        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        private IMenuSelector MenuSelector { get; }
        private IConversionViewModel ConversionViewModel { get; }

        public string Header     => CurrencyViewModel.Header;
        public Currency Currency => CurrencyViewModel.Currency;

        public Brush Background => IsSelected
            ? CurrencyViewModel.IconBrush
            : CurrencyViewModel.UnselectedIconBrush;

        public Brush OpacityMask => IsSelected
            ? (CurrencyViewModel.IconBrush is ImageBrush ? null : CurrencyViewModel.IconMaskBrush)
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
            Currency currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            MenuSelector = menuSelector ?? throw new ArgumentNullException(nameof(menuSelector));
            ConversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));

            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);

            SubscribeToServices();

            LoadTransactionsAsync().FireAndForget();
        }

        private void SubscribeToServices()
        {
            App.Account.BalanceUpdated += async (sender, args) =>
            {
                try
                {
                    if (Currency.Name == args.Currency.Name)
                        await LoadTransactionsAsync();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Account balance updated event handler error");
                }
            };
        }

        private async Task LoadTransactionsAsync()
        {
            Log.Debug("LoadTransactionsAsync for {@currency}", Currency.Name);

            if (App.Account == null)
                return;

            var transactions = (await App.Account
                .GetTransactionsAsync(Currency))
                .ToList();

            var outputs = await App.Account
                .GetOutputsAsync(Currency);

            var factory = new TransactionViewModelCreator(transactions, outputs);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Transactions = new ObservableCollection<TransactionViewModel>(
                    transactions.Select(t => factory
                        .CreateViewModel(t))
                        .ToList()
                        .SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));

            }, DispatcherPriority.Background);
        }

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ?? (_sendCommand = new Command(OnSendClick));

        private ICommand _receiveCommand;
        public ICommand ReceiveCommand => _receiveCommand ?? (_receiveCommand = new Command(OnReceiveClick));

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ?? (_convertCommand = new Command(OnConvertClick));

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new Command(OnUpdateClick));

        private ICommand _cancelUpdateCommand;
        public ICommand CancelUpdateCommand => _cancelUpdateCommand ?? (_cancelUpdateCommand = new Command(() =>
        {
            Cancellation.Cancel();
        }));

        private void OnSendClick()
        {
            var viewModel = new SendViewModel(App, DialogViewer, Currency);

            DialogViewer?.ShowSendDialog(viewModel, dialogLoaded: () =>
            {
                viewModel.Show();
            });
        }

        private void OnReceiveClick()
        {
            DialogViewer?.ShowReceiveDialog(new ReceiveViewModel(App)
            {
                Currency = Currency
            });
        }

        private void OnConvertClick()
        {
            MenuSelector.SelectMenu(ConversionViewIndex);
            ConversionViewModel.FromCurrency = Currency;
        }

        private async void OnUpdateClick()
        {
            IsBalanceUpdating = true;

            Cancellation = new CancellationTokenSource();

            try
            {
                var scanner = new HdWalletScanner(App.Account);

                await scanner.ScanAsync(
                    currency: Currency,
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

        private void DesignerMode()
        {
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(DesignTime.Currencies[0], subscribeToUpdates: false);
            CurrencyViewModel.TotalAmount             = 0.01012345m;
            CurrencyViewModel.TotalAmountInBase       = 16.51m;
            CurrencyViewModel.AvailableAmount         = 0.01010005m;
            CurrencyViewModel.AvailableAmountInBase   = 16.00m;
            CurrencyViewModel.UnconfirmedAmount       = 0.00002m;
            CurrencyViewModel.UnconfirmedAmountInBase = 0.5m;
            //CurrencyViewModel.LockedAmount            = 0.00000340m;
            //CurrencyViewModel.LockedAmountInBase      = 0.01m;

            var transactions = new List<TransactionViewModel>
            {
                new TransactionViewModel
                {
                    Currency     = DesignTime.Currencies.Get<Bitcoin>(),
                    Id           = "064ca0c2d0b94d5ef42c0c01f583f7889f9a4cac04c7558ff16e834921d3e699",
                    Type         = TransactionType.Sent,
                    Description  = "Sent 0.00124 BTC",
                    Amount       = -0.00124m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    State        = TransactionState.Unconfirmed,
                    Time         = DateTime.Now,
                },
                new TransactionViewModel
                {
                    Currency     = DesignTime.Currencies.Get<Bitcoin>(),
                    Id           = "064ca0c2d0b94d5ef42c0c01f583f7889f9a4cac04c7558ff16e834921d3e699",
                    Type         = TransactionType.Received,
                    Description  = "Received 1.00666 BTC",
                    Amount       = 1.00666m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    State        = TransactionState.Confirmed,
                    Time         = DateTime.Now,
                }
            };

            Transactions = new ObservableCollection<TransactionViewModel>(
                transactions.SortList((t1, t2) => t2.Time.CompareTo(t1.Time)));
        }
    }
}

//new TransactionViewModel
//{
//    Type        = TransactionType.Exchanged,
//    Description = "Exchanged 1 BTC from 20 LTC",
//    Amount      = 1m,
//    State       = TransactionState.Confirmed,
//    Time        = DateTime.Now,
//},
//new TransactionViewModel
//{
//    Type        = TransactionType.Exchanged,
//    Description = "Exchanged 1 BTC to 25 LTC",
//    Amount      = -1m,
//    State       = TransactionState.Confirmed,
//    Time        = DateTime.Now,
//},
//new TransactionViewModel
//{
//    Type        = TransactionType.Refunded,
//    Description = "Refunded 1 BTC",
//    Amount      = 1m, 
//    State       = TransactionState.Confirmed,
//    Time        = DateTime.Now.AddDays(-1),
//},
//new TransactionViewModel
//{
//    Type        = TransactionType.Unknown,
//    Description = "Unknown",
//    Amount      = 0.001234m,
//    State       = TransactionState.Unconfirmed,
//    Time        = DateTime.Now.AddDays(-3),
//}