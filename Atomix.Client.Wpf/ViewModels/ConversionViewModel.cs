using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomix.Common;
using Atomix.Core;
using Atomix.Core.Entities;
using Atomix.MarketData;
using Atomix.MarketData.Abstract;
using Atomix.Subsystems;
using Atomix.Subsystems.Abstract;
using Atomix.Swaps;
using Atomix.Wallet.Abstract;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Client.Wpf.Properties;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels
{
    public class ConversionOrderType
    {
        public string Description { get; set; }
        public OrderType OrderType { get; set; }

        public static ConversionOrderType Standard => new ConversionOrderType
        {
            OrderType = OrderType.FillOrKill,
            Description = Resources.CvOrderTypeStandard
        };

        //public static ConversionOrderType StandardWithFixedFee => new ConversionOrderType
        //{
        //    OrderType = OrderType.DirectFillOrKill,
        //    Description = Resources.CvOrderTypeStandardWithFixedFee
        //};
    }

    public class ConversionViewModel : BaseViewModel, IConversionViewModel
    {
        public IDialogViewer DialogViewer { get; set; }

        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private List<CurrencyViewModel> _toCurrencies;
        public List<CurrencyViewModel> ToCurrencies
        {
            get => _toCurrencies;
            private set { _toCurrencies = value; OnPropertyChanged(nameof(ToCurrencies)); }
        }

        public List<ConversionOrderType> OrderTypes => new List<ConversionOrderType>
        {
            ConversionOrderType.Standard,
            //ConversionOrderType.StandardWithFixedFee
        };

        private Currency _fromCurrency;
        public Currency FromCurrency
        {
            get => _fromCurrency;
            set
            {
                _fromCurrency = value;
                OnPropertyChanged(nameof(FromCurrency));

                if (_fromCurrency == null)
                    return;

                if (_fromCurrency == _toCurrency)
                    ToCurrency = ToCurrencies.First(c => c.Currency != _fromCurrency).Currency;

                FromCurrencyViewModel = FromCurrencies.First(c => c.Currency.Equals(_fromCurrency));
                Amount = 0;
                Fee = 0;
            }
        }

        private Currency _toCurrency;
        public Currency ToCurrency
        {
            get => _toCurrency;
            set
            {
                _toCurrency = value;
                OnPropertyChanged(nameof(ToCurrency));

                if (_toCurrency == null)
                    return;

                if (_toCurrency == _fromCurrency)
                    FromCurrency = FromCurrencies.First(c => c.Currency != _toCurrency).Currency;

                ToCurrencyViewModel = ToCurrencies.First(c => c.Currency.Equals(_toCurrency));
            }
        }

        private CurrencyViewModel _fromCurrencyViewModel;
        public CurrencyViewModel FromCurrencyViewModel
        {
            get => _fromCurrencyViewModel;
            set
            {
                _fromCurrencyViewModel = value;
                OnPropertyChanged(nameof(FromCurrencyViewModel));

                CurrencyFormat = _fromCurrencyViewModel?.CurrencyFormat;
                CurrencyCode = _fromCurrencyViewModel?.CurrencyCode;
                BaseCurrencyFormat = _fromCurrencyViewModel?.BaseCurrencyFormat;
                BaseCurrencyCode = _fromCurrencyViewModel?.BaseCurrencyCode;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol != null)
                    PriceFormat = $"F{symbol.Quote.Digits}";
            }
        }

        private CurrencyViewModel _toCurrencyViewModel;
        public CurrencyViewModel ToCurrencyViewModel
        {
            get => _toCurrencyViewModel;
            set
            {
                _toCurrencyViewModel = value;
                OnPropertyChanged(nameof(ToCurrencyViewModel));

                TargetCurrencyFormat = _toCurrencyViewModel?.CurrencyFormat;
                TargetCurrencyCode = _toCurrencyViewModel?.CurrencyCode;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol != null)
                    PriceFormat = $"F{symbol.Quote.Digits}";
            }
        }

        private OrderType _orderType;
        public OrderType OrderType
        {
            get => _orderType;
            set { _orderType = value; OnPropertyChanged(nameof(OrderType)); }
        }

        private string _currencyFormat;
        public string CurrencyFormat
        {
            get => _currencyFormat;
            set
            {
                if (value == null)
                    return;

                _currencyFormat = value;
                OnPropertyChanged(CurrencyFormat);
            }
        }

        private string _targetCurrencyFormat;
        public string TargetCurrencyFormat
        {
            get => _targetCurrencyFormat;
            set { _targetCurrencyFormat = value; OnPropertyChanged(TargetCurrencyFormat); }
        }

        private string _baseCurrencyFormat;
        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;

                if (_amount + _fee > FromCurrencyViewModel.AvailableAmount)
                    _amount = FromCurrencyViewModel.AvailableAmount - _fee;

                OnPropertyChanged(nameof(CurrencyFormat));
                OnPropertyChanged(nameof(Amount));
                
#if DEBUG
                if (!Env.IsInDesignerMode()) {
#endif
                    OnQuotesUpdatedEventHandler(App.AtomixApp.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(App.AtomixApp.QuotesProvider, EventArgs.Empty);
#if DEBUG
                }
#endif
            }
        }

        private decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                if (_fee + _amount > FromCurrencyViewModel.AvailableAmount)
                    _fee = FromCurrencyViewModel.AvailableAmount - _amount;

                OnPropertyChanged(nameof(CurrencyFormat));
                OnPropertyChanged(nameof(Fee));
#if DEBUG
                if (!Env.IsInDesignerMode()) {
#endif
                    OnBaseQuotesUpdatedEventHandler(App.AtomixApp.QuotesProvider, EventArgs.Empty);
#if DEBUG
                }
#endif
            }
        }

        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        private decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        private decimal _targetAmount;
        public decimal TargetAmount
        {
            get => _targetAmount;
            set { _targetAmount = value; OnPropertyChanged(nameof(TargetAmount)); }
        }

        private decimal _targetAmountInBase;
        public decimal TargetAmountInBase
        {
            get => _targetAmountInBase;
            set { _targetAmountInBase = value; OnPropertyChanged(nameof(TargetAmountInBase)); }
        }

        private string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        private string _targetCurrencyCode;
        public string TargetCurrencyCode
        {
            get => _targetCurrencyCode;
            set { _targetCurrencyCode = value; OnPropertyChanged(nameof(TargetCurrencyCode)); }
        }

        private string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        private decimal _estimatedPrice;
        public decimal EstimatedPrice
        {
            get => _estimatedPrice;
            set { _estimatedPrice = value; OnPropertyChanged(nameof(EstimatedPrice)); }
        }

        private decimal _estimatedFee;
        public decimal EstimatedFee
        {
            get => _estimatedFee;
            set { _estimatedFee = value; OnPropertyChanged(nameof(EstimatedFee)); }
        }

        private decimal _serviceFee;
        public decimal ServiceFee
        {
            get => _serviceFee;
            set { _serviceFee = value; OnPropertyChanged(nameof(ServiceFee)); }
        }

        private string _priceFormat;
        public string PriceFormat
        {
            get => _priceFormat;
            set { _priceFormat = value; OnPropertyChanged(nameof(PriceFormat)); }
        }

        private ObservableCollection<SwapViewModel> _swaps;
        public ObservableCollection<SwapViewModel> Swaps
        {
            get => _swaps;
            set { _swaps = value; OnPropertyChanged(nameof(Swaps)); }
        }

        public ConversionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ConversionViewModel(IDialogViewer dialogViewer)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            SubscribeToServices(App.AtomixApp);
        }

        private void SubscribeToServices(AtomixApp app)
        {
            app.AccountChanged += OnAccountChangedEventHandler;

            if (app.HasQuotesProvider)
                app.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;

            app.Terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            app.Terminal.ExecutionReportReceived += OnExecutionReportEventHandler;
            app.Terminal.SwapUpdated += OnSwapEventHandler;
        }

        private void OnAccountChangedEventHandler(object sender, AccountChangedEventArgs args)
        {
            if (!(sender is AtomixApp))
                return;

            if (args.NewAccount == null)
                return;

            var account = args.NewAccount;
            account.SwapsLoaded += (o, eventArgs) => OnSwapEventHandler(o, new SwapEventArgs());

            FromCurrencies = account.Wallet.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            ToCurrencies = account.Wallet.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            OrderType = OrderType.FillOrKill;
            FromCurrency = Currencies.Btc;
            ToCurrency = Currencies.Ltc;

            OnSwapEventHandler(this, new SwapEventArgs());
        }

        private void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (CurrencyCode == null || BaseCurrencyCode == null)
                return;

            var quote = provider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = _amount * (quote?.Bid ?? 0m);
            FeeInBase    = _fee * (quote?.Bid ?? 0m);

            UpdateTargetAmountInBase(provider);
        }

        private void UpdateTargetAmountInBase(ICurrencyQuotesProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (TargetCurrencyCode == null)
                return;

            if (BaseCurrencyCode == null)
                return;

            var quote = provider.GetQuote(TargetCurrencyCode, BaseCurrencyCode);

            TargetAmountInBase = _targetAmount * (quote?.Bid ?? 0m);
        }

        private void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            if (!(sender is ITerminal terminal))
                return;

            if (ToCurrency == null)
                return;

            var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
            if (symbol == null)
                return;  

            var side = symbol.OrderSideForBuyCurrency(ToCurrency);
            var orderBook = terminal.GetOrderBook(symbol);

            _estimatedPrice = orderBook.EstimatedDealPrice(side, Amount);

            if (symbol.IsBaseCurrency(ToCurrency))
            {
                _targetAmount = _estimatedPrice != 0
                    ? Amount / _estimatedPrice
                    : 0m;
            }
            else if (symbol.IsQuoteCurrency(ToCurrency))
            {
                _targetAmount = Amount * _estimatedPrice;
            }

            //Application.Current.Dispatcher.Invoke(() =>
            //{
                OnPropertyChanged(nameof(EstimatedPrice));
                OnPropertyChanged(nameof(PriceFormat));

                OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(TargetAmount));

                UpdateTargetAmountInBase(App.AtomixApp.QuotesProvider);

            //}, DispatcherPriority.Background);
        }

        private void OnExecutionReportEventHandler(object sender, ExecutionReportEventArgs args)
        {

        }

        private async void OnSwapEventHandler(object sender, SwapEventArgs args)
        {
            try
            {
                var swaps = await App.AtomixApp.Account
                    .GetSwapsAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var swapViewModels = swaps
                        .Select(SwapViewModelFactory.CreateSwapViewModel)
                        .ToList()
                        .SortList(((s1, s2) => s2.Time.CompareTo(s1.Time)));

                    Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

                }, DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "Swaps update error");
            }
        }

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ?? (_convertCommand = new Command(OnConvertClick));

        private async void OnConvertClick()
        {
            if (Amount == 0) {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvWrongAmount);
                return;
            }

            var terminal = App.AtomixApp.Terminal;

            if (!terminal.IsServiceConnected(TerminalService.All)) {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvServicesUnavailable);
                return;
            }

            var account = App.AtomixApp.Account;

            try
            {
                if (account.IsLocked)
                {
                    await UnlockAccountAsync(account);

                    if (account.IsLocked) {
                        DialogViewer.ShowMessage(Resources.CvError, Resources.CvWalletLocked);
                        return;
                    }
                }

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol == null) {
                    DialogViewer.ShowMessage(Resources.CvError, Resources.CvNotSupportedSymbol);
                    return;
                }

                var requiredAmount = Amount + Fee;

                var fromWallets = await account
                    .GetUnspentAddressesAsync(
                        currency: FromCurrency,
                        requiredAmount: requiredAmount); 

                var refundWallet = await account
                    .GetRefundAddressAsync(FromCurrency, fromWallets);

                var toWallet = await account
                    .GetRedeemAddressAsync(ToCurrency);

                var side = symbol.OrderSideForBuyCurrency(ToCurrency);
                var orderBook = terminal.GetOrderBook(symbol);
                var price = orderBook.EstimatedDealPrice(side, Amount);

                if (price == 0) {
                    DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvNoLiquidity);
                    return;
                }

                var qty = Math.Round(AmountHelper.AmountToQty(side, Amount, price), symbol.QtyDigits);

                var order = new Order
                {
                    Symbol       = symbol,
                    TimeStamp    = DateTime.UtcNow,
                    Price        = price,
                    Qty          = qty,
                    Fee          = Fee,
                    Side         = side,
                    Type         = OrderType,
                    FromWallets  = fromWallets.ToList(),
                    ToWallet     = toWallet,
                    RefundWallet = refundWallet
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");

                DialogViewer.ShowMessage(Resources.CvError, Resources.CvConversionError);
            }
        }

        private Task UnlockAccountAsync(IAccount account)
        {
            var viewModel = new UnlockViewModel(
                walletName: "wallet",
                unlockAction: password => Task.Factory.StartNew(() => {
                    account.Unlock(password);
                }));

            viewModel.Unlocked += (sender, args) => DialogViewer?.HideUnlockDialog();

            return DialogViewer?.ShowUnlockDialogAsync(viewModel);
        }

        private void DesignerMode()
        {
            FromCurrencies = new List<CurrencyViewModel>
            {
                CurrencyViewModelCreator.CreateViewModel(Currencies.Btc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(Currencies.Ltc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(Currencies.Eth, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(Currencies.Xtz, subscribeToUpdates: false)
            };

            ToCurrencies = new List<CurrencyViewModel>
            {
                CurrencyViewModelCreator.CreateViewModel(Currencies.Btc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(Currencies.Ltc, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(Currencies.Eth, subscribeToUpdates: false),
                CurrencyViewModelCreator.CreateViewModel(Currencies.Xtz, subscribeToUpdates: false)
            };

            OrderType = OrderType.FillOrKill;
            FromCurrency = Currencies.Btc;
            ToCurrency = Currencies.Ltc;

            var swapViewModels = new List<SwapViewModel>
            {
                SwapViewModelFactory.CreateSwapViewModel(new SwapState
                {
                    Order = new Order
                    {
                        Symbol = Symbols.LtcBtc,
                        LastPrice = 0.0000888m,
                        LastQty = 0.001000m,
                        Side = Side.Buy,
                        TimeStamp = DateTime.UtcNow,
                        SwapId = Guid.NewGuid() 
                    }
                }),
                SwapViewModelFactory.CreateSwapViewModel(new SwapState
                {
                    Order = new Order
                    {
                        Symbol = Symbols.LtcBtc,
                        LastPrice = 0.0100808m,
                        LastQty = 0.0043000m,
                        Side = Side.Sell,
                        TimeStamp = DateTime.UtcNow,
                        SwapId = Guid.NewGuid()
                    }
                })
            };

            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);
        }
    }
}