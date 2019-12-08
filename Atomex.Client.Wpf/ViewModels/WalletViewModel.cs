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
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Common;
using Atomex.Core.Entities;
using Atomex.Wallet;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.SendViewModels;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;
using NBitcoin;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class Delegation
    {
        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
    }

    public class WalletViewModel : BaseViewModel
    {
        private const int ConversionViewIndex = 2;
        private const int DelegationCheckIntervalInSec = 20;

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

        public string Header => CurrencyViewModel.Header;
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

        public bool IsDelegatable => Currency != null && Currency is Tezos;

        private bool _canDelegate;
        public bool CanDelegate
        {
            get => _canDelegate;
            set { _canDelegate = value; OnPropertyChanged(nameof(CanDelegate)); }
        }

        private bool _hasDelegations;
        public bool HasDelegations
        {
            get => _hasDelegations;
            set { _hasDelegations = value; OnPropertyChanged(nameof(HasDelegations)); }
        }

        private List<Delegation> _delegations;
        public List<Delegation> Delegations
        {
            get => _delegations;
            set { _delegations = value; OnPropertyChanged(nameof(Delegations)); }
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
            Delegations = new List<Delegation>();

            SubscribeToServices();

            // update transactions list
            LoadTransactionsAsync().FireAndForget();

            // update delegation info
            if (IsDelegatable)
                LoadDelegationInfoAsync().FireAndForget();
        }

        private void SubscribeToServices()
        {
            App.Account.BalanceUpdated += async (sender, args) =>
            {
                try
                {
                    if (Currency.Name == args.Currency.Name)
                    {
                        // update transactions list
                        await LoadTransactionsAsync();

                        // update delegation info
                        if (IsDelegatable)
                            await LoadDelegationInfoAsync();
                    }
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

            try
            {
                if (App.Account == null)
                    return;

                var transactions = (await App.Account
                    .GetTransactionsAsync(Currency))
                    .ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions = new ObservableCollection<TransactionViewModel>(
                        transactions.Select(t => TransactionViewModelCreator
                            .CreateViewModel(t))
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
            catch (Exception e)
            {
                Log.Error(e, "LoadTransactionAsync error for {@currency}", Currency?.Name);
            }
        }

        private async Task LoadDelegationInfoAsync()
        {
            try
            {
                var tezos = Currency as Tezos;

                var balance = await App.Account
                    .GetBalanceAsync(tezos)
                    .ConfigureAwait(false);

                var addresses = await App.Account
                    .GetUnspentAddressesAsync(tezos)
                    .ConfigureAwait(false);

                var rpc = new Rpc(tezos.RpcNodeUri);

                var delegations = new List<Delegation>();

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                        continue;


                    var baker = await new BbApi(tezos)
                        .GetBaker(@delegate, App.Account.Network)
                        .ConfigureAwait(false);

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance
                    });
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CanDelegate = balance.Available > 0;
                    Delegations = delegations;
                    HasDelegations = delegations.Count > 0;
                },
                DispatcherPriority.Background); 
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error");
            }
        }

        private ICommand _sendCommand;
        public ICommand SendCommand => _sendCommand ?? (_sendCommand = new Command(OnSendClick));

        private ICommand _receiveCommand;
        public ICommand ReceiveCommand => _receiveCommand ?? (_receiveCommand = new Command(OnReceiveClick));

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ?? (_convertCommand = new Command(OnConvertClick));

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new Command(OnUpdateClick));

        private ICommand _delegateCommand;
        public ICommand DelegateCommand => _delegateCommand ?? (_delegateCommand = new Command(OnDelegateClick));

        private ICommand _cancelUpdateCommand;
        public ICommand CancelUpdateCommand => _cancelUpdateCommand ?? (_cancelUpdateCommand = new Command(() =>
        {
            Cancellation.Cancel();
        }));

        private void OnSendClick()
        {
            var viewModel = new SendViewModel(App, DialogViewer, Currency);

            DialogViewer?.ShowSendDialog(viewModel, dialogLoaded: viewModel.Show);
        }

        private void OnReceiveClick() =>
            DialogViewer?.ShowReceiveDialog(new ReceiveViewModel(App, Currency));

        private void OnConvertClick()
        {
            MenuSelector.SelectMenu(ConversionViewIndex);
            ConversionViewModel.FromCurrency = Currency;
        }

        private async void OnUpdateClick()
        {
            if (IsBalanceUpdating)
                return;

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

        private void OnDelegateClick()
        {
            var viewModel = new DelegateViewModel(App, DialogViewer, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(DelegationCheckIntervalInSec))
                    .ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(OnUpdateClick);
            });

            DialogViewer?.ShowDelegateDialog(viewModel, dialogLoaded: viewModel.Show);
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
                var isRemoved = await App.Account
                    .RemoveTransactionAsync(args.Transaction.Id);

                if (isRemoved)
                    await LoadTransactionsAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Transaction remove error");
            }
        }

        private void DesignerMode()
        {
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(DesignTime.Currencies[3], subscribeToUpdates: false);
            CurrencyViewModel.TotalAmount             = 0.01012345m;
            CurrencyViewModel.TotalAmountInBase       = 16.51m;
            CurrencyViewModel.AvailableAmount         = 0.01010005m;
            CurrencyViewModel.AvailableAmountInBase   = 16.00m;
            CurrencyViewModel.UnconfirmedAmount       = 0.00002m;
            CurrencyViewModel.UnconfirmedAmountInBase = 0.5m;

            var transactions = new List<TransactionViewModel>
            {
                new BitcoinBasedTransactionViewModel(new BitcoinBasedTransaction(DesignTime.Currencies.Get<Bitcoin>(), Transaction.Create(Network.TestNet)))
                {
                    Description  = "Sent 0.00124 BTC",
                    Amount       = -0.00124m,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Time         = DateTime.Now,
                },
                new BitcoinBasedTransactionViewModel(new BitcoinBasedTransaction(DesignTime.Currencies.Get<Bitcoin>(), Transaction.Create(Network.TestNet)))
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

            Delegations = new List<Delegation>()
            {
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                }
            };

            HasDelegations = true;
        }
    }
}