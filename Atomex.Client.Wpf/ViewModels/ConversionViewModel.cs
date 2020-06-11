using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Abstract;
using Atomex.Blockchain.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using Atomex.Swaps;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ConversionViewModel : BaseViewModel, IConversionViewModel
    {
        protected IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }

        private decimal _estimatedOrderPrice;

        private ISymbols Symbols
        {
            get
            {
#if DEBUG
                if (Env.IsInDesignerMode())
                    return DesignTime.Symbols;
#endif
                return App.Account.Symbols;
            }
        }

        private ICurrencies Currencies
        {
            get
            {
#if DEBUG
                if (Env.IsInDesignerMode())
                    return DesignTime.Currencies;
#endif
                return App.Account.Currencies;
            }
        }

        private List<CurrencyViewModel> _currencyViewModels;

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

        protected Currency _fromCurrency;
        public virtual Currency FromCurrency
        {
            get => _fromCurrency;
            set
            {
                _fromCurrency = FromCurrencies
                    .FirstOrDefault(c => c.Currency.Name == value?.Name)
                    ?.Currency;

                OnPropertyChanged(nameof(FromCurrency));

                if (_fromCurrency == null)
                    return;

                var oldToCurrency = ToCurrency;

                ToCurrencies = _currencyViewModels
                    .Where(c => Symbols.SymbolByCurrencies(c.Currency, _fromCurrency) != null)
                    .ToList();

                if (oldToCurrency != null &&
                    oldToCurrency.Name != _fromCurrency.Name &&
                    ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrency.Name) != null)
                {
                    ToCurrency = ToCurrencies
                        .FirstOrDefault(c => c.Currency.Name ==  oldToCurrency.Name)
                        .Currency;
                }
                else
                {
                    ToCurrency = ToCurrencies
                        .First()
                        .Currency;
                }

                FromCurrencyViewModel = _currencyViewModels
                    .First(c => c.Currency.Name == _fromCurrency.Name);

                Amount = 0;
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

                ToCurrencyViewModel = _currencyViewModels.First(c => c.Currency.Name == _toCurrency.Name);

#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    OnQuotesUpdatedEventHandler(App.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
#if DEBUG
                }
#endif
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

                FromFeeCurrencyCode = _fromCurrencyViewModel?.FeeCurrencyCode;
                FromFeeCurrencyFormat = _fromCurrencyViewModel?.FeeCurrencyFormat;

                BaseCurrencyFormat = _fromCurrencyViewModel?.BaseCurrencyFormat;
                BaseCurrencyCode = _fromCurrencyViewModel?.BaseCurrencyCode;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol != null)
                {
                    var quoteCurrency = Currencies.GetByName(symbol.Quote);

                    PriceFormat = $"F{quoteCurrency.Digits}";
                }
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

                TargetFeeCurrencyCode = _toCurrencyViewModel?.FeeCurrencyCode;
                TargetFeeCurrencyFormat = _toCurrencyViewModel?.FeeCurrencyFormat;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol != null)
                {
                    var quoteCurrency = Currencies.GetByName(symbol.Quote);

                    PriceFormat = $"F{quoteCurrency.Digits}";
                }

                UpdateRedeemAndRewardFeesAsync();
            }
        }

        private string _priceFormat;
        public string PriceFormat
        {
            get => _priceFormat;
            set { _priceFormat = value; OnPropertyChanged(nameof(PriceFormat)); }
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

        protected decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { UpdateAmount(value); }
        }

        private decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set { _isAmountUpdating = value; OnPropertyChanged(nameof(IsAmountUpdating)); }
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

        private string _fromFeeCurrencyFormat;
        public string FromFeeCurrencyFormat
        {
            get => _fromFeeCurrencyFormat;
            set { _fromFeeCurrencyFormat = value; OnPropertyChanged(nameof(FromFeeCurrencyFormat)); }
        }

        private string _fromFeeCurrencyCode;
        public string FromFeeCurrencyCode
        {
            get => _fromFeeCurrencyCode;
            set { _fromFeeCurrencyCode = value; OnPropertyChanged(nameof(FromFeeCurrencyCode)); }
        }

        private string _targetCurrencyCode;
        public string TargetCurrencyCode
        {
            get => _targetCurrencyCode;
            set { _targetCurrencyCode = value; OnPropertyChanged(nameof(TargetCurrencyCode)); }
        }

        private string _targetFeeCurrencyCode;
        public string TargetFeeCurrencyCode
        {
            get => _targetFeeCurrencyCode;
            set { _targetFeeCurrencyCode = value; OnPropertyChanged(nameof(TargetFeeCurrencyCode)); }
        }

        private string _targetFeeCurrencyFormat;
        public string TargetFeeCurrencyFormat
        {
            get => _targetFeeCurrencyFormat;
            set { _targetFeeCurrencyFormat = value; OnPropertyChanged(nameof(TargetFeeCurrencyFormat)); }
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

        private decimal _estimatedMaxAmount;
        public decimal EstimatedMaxAmount
        {
            get => _estimatedMaxAmount;
            set { _estimatedMaxAmount = value; OnPropertyChanged(nameof(EstimatedMaxAmount)); }
        }

        protected decimal _estimatedPaymentFee;
        public decimal EstimatedPaymentFee
        {
            get => _estimatedPaymentFee;
            set { _estimatedPaymentFee = value; OnPropertyChanged(nameof(EstimatedPaymentFee)); }
        }

        private decimal _estimatedPaymentFeeInBase;
        public decimal EstimatedPaymentFeeInBase
        {
            get => _estimatedPaymentFeeInBase;
            set { _estimatedPaymentFeeInBase = value; OnPropertyChanged(nameof(EstimatedPaymentFeeInBase)); }
        }

        private decimal _estimatedRedeemFee;
        public decimal EstimatedRedeemFee
        {
            get => _estimatedRedeemFee;
            set { _estimatedRedeemFee = value; OnPropertyChanged(nameof(EstimatedRedeemFee)); }
        }

        private decimal _estimatedRedeemFeeInBase;
        public decimal EstimatedRedeemFeeInBase
        {
            get => _estimatedRedeemFeeInBase;
            set { _estimatedRedeemFeeInBase = value; OnPropertyChanged(nameof(EstimatedRedeemFeeInBase)); }
        }

        private decimal _rewardForRedeem;
        public decimal RewardForRedeem
        {
            get => _rewardForRedeem;
            set
            {
                _rewardForRedeem = value;
                OnPropertyChanged(nameof(RewardForRedeem));

                HasRewardForRedeem = _rewardForRedeem != 0;
            }
        }

        private decimal _rewardForRedeemInBase;
        public decimal RewardForRedeemInBase
        {
            get => _rewardForRedeemInBase;
            set { _rewardForRedeemInBase = value; OnPropertyChanged(nameof(RewardForRedeemInBase)); }
        }

        private bool _hasRewardForRedeem;
        public bool HasRewardForRedeem
        {
            get => _hasRewardForRedeem;
            set { _hasRewardForRedeem = value; OnPropertyChanged(nameof(HasRewardForRedeem)); }
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private ObservableCollection<SwapViewModel> _swaps;
        public ObservableCollection<SwapViewModel> Swaps
        {
            get => _swaps;
            set { _swaps = value; OnPropertyChanged(nameof(Swaps)); }
        }

        private bool _isNoLiquidity;
        public bool IsNoLiquidity
        {
            get => _isNoLiquidity;
            set { _isNoLiquidity = value; OnPropertyChanged(nameof(IsNoLiquidity)); }
        }

        public ConversionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public ConversionViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            SubscribeToServices();
        }

        private ICommand _convertCommand;
        public ICommand ConvertCommand => _convertCommand ?? (_convertCommand = new Command(OnConvertClick));

        private ICommand _maxAmountCommand;
        public ICommand MaxAmountCommand => _maxAmountCommand ?? (_maxAmountCommand = new Command(() =>
        {
            Amount = EstimatedMaxAmount;
        }));

        private void SubscribeToServices()
        {
            App.TerminalChanged += OnTerminalChangedEventHandler;

            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;
        }

        protected virtual async void UpdateAmount(decimal value)
        {
            Warning = string.Empty;

            try
            {
                IsAmountUpdating = true;

                var previousAmount = _amount;
                _amount = value;

                var (maxAmount, maxFee, reserve) = await App.Account
                    .EstimateMaxAmountToSendAsync(FromCurrency.Name, null, BlockchainTransactionType.SwapPayment);

                var swaps = await App.Account
                    .GetSwapsAsync();

                var usedAmount = swaps.Sum(s => (s.IsActive && s.SoldCurrency == FromCurrency.Name && !s.StateFlags.HasFlag(SwapStateFlags.IsPaymentConfirmed))
                    ? s.Symbol.IsBaseCurrency(FromCurrency.Name)
                        ? s.Qty
                        : s.Qty * s.Price
                    : 0);

                usedAmount = AmountHelper.RoundDown(usedAmount, FromCurrency.DigitsMultiplier);

                maxAmount = Math.Max(maxAmount - usedAmount, 0);

                var includeFeeToAmount = FromCurrency.FeeCurrencyName == FromCurrency.Name;

                var availableAmount = FromCurrency is BitcoinBasedCurrency
                    ? FromCurrencyViewModel.AvailableAmount
                    : maxAmount + (includeFeeToAmount ? maxFee : 0);

                var estimatedPaymentFee = _amount != 0
                    ? (_amount < availableAmount
                        ? await App.Account
                            .EstimateFeeAsync(FromCurrency.Name, null, _amount, BlockchainTransactionType.SwapPayment)
                        : null)
                    : 0;

                if (estimatedPaymentFee == null)
                {
                    if (maxAmount > 0)
                    {
                        _amount = maxAmount;
                        estimatedPaymentFee = maxFee;
                    }
                    else
                    {
                        _amount = 0; // previousAmount;
                        OnPropertyChanged(nameof(Amount));

                        if(FromCurrency.Name != FromCurrency.FeeCurrencyName && FromCurrencyViewModel.AvailableAmount > 0)                        
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, FromCurrency.FeeCurrencyName);

                        IsAmountUpdating = false;
                        return;

                        
                        // todo: insufficient funds warning
                        // 
                    }
                }

                EstimatedPaymentFee = estimatedPaymentFee.Value;

                if (_amount + (includeFeeToAmount ? _estimatedPaymentFee : 0) > availableAmount)
                    _amount = Math.Max(availableAmount - (includeFeeToAmount ? _estimatedPaymentFee : 0), 0);

                OnPropertyChanged(nameof(CurrencyFormat));
                OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(Amount));

                UpdateRedeemAndRewardFeesAsync();

                //var walletAddress = await App.Account
                //    .GetRedeemAddressAsync(ToCurrency.FeeCurrencyName);
                //EstimatedRedeemFee = ToCurrency.GetRedeemFee(walletAddress);

                //RewardForRedeem = walletAddress.AvailableBalance() < EstimatedRedeemFee && !(ToCurrency is BitcoinBasedCurrency)
                //    ? ToCurrency.GetRewardForRedeem()
                //    : 0;

                //OnPropertyChanged(nameof(EstimatedPaymentFee));
                //OnPropertyChanged(nameof(EstimatedRedeemFee));
                //OnPropertyChanged(nameof(RewardForRedeem));

#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    OnQuotesUpdatedEventHandler(App.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
#if DEBUG
                }
#endif
            }
            finally
            {
                IsAmountUpdating = false;
            }
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

        protected async void UpdateRedeemAndRewardFeesAsync()
        {
#if DEBUG
            if (!Env.IsInDesignerMode())
            {
#endif
                var walletAddress = await App.Account
                    .GetRedeemAddressAsync(ToCurrency.FeeCurrencyName);

                EstimatedRedeemFee = ToCurrency.GetRedeemFee(walletAddress);

                RewardForRedeem = walletAddress.AvailableBalance() < EstimatedRedeemFee && !(ToCurrency is BitcoinBasedCurrency)
                    ? ToCurrency.GetRewardForRedeem()
                    : 0;
#if DEBUG
            }
#endif
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs args)
        {
            var terminal = args.Terminal;

            if (terminal?.Account == null)
                return;

            terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            terminal.SwapUpdated += OnSwapEventHandler;

            _currencyViewModels = terminal.Account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            FromCurrencies = _currencyViewModels.ToList();
            FromCurrency = terminal.Account.Currencies.GetByName("BTC");
            ToCurrency = terminal.Account.Currencies.GetByName("LTC");

            OnSwapEventHandler(this, null);
        }

        protected void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (CurrencyCode == null || TargetCurrencyCode == null || BaseCurrencyCode == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(CurrencyCode, BaseCurrencyCode)?.Bid ?? 0m;
            AmountInBase = _amount * fromCurrencyPrice;

            var fromCurrencyFeePrice = provider.GetQuote(FromCurrency.FeeCurrencyName, BaseCurrencyCode)?.Bid ?? 0m;
            EstimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyFeePrice;

            var toCurrencyFeePrice = provider.GetQuote(ToCurrency.FeeCurrencyName, BaseCurrencyCode)?.Bid ?? 0m;
            EstimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyFeePrice;

            var toCurrencyPrice = provider.GetQuote(TargetCurrencyCode, BaseCurrencyCode)?.Bid ?? 0m;
            RewardForRedeemInBase = _rewardForRedeem * toCurrencyPrice;

            UpdateTargetAmountInBase(provider);
        }

        protected async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
            {
                if (!(sender is IAtomexClient terminal))
                    return;

                if (ToCurrency == null)
                    return;

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                if (symbol == null)
                    return;

                var side = symbol.OrderSideForBuyCurrency(ToCurrency);
                var orderBook = terminal.GetOrderBook(symbol);

                if (orderBook == null)
                    return;

                var walletAddress = await App.Account
                    .GetRedeemAddressAsync(ToCurrency.FeeCurrencyName);

                var baseCurrency = Currencies.GetByName(symbol.Base);

                (_estimatedOrderPrice, _estimatedPrice) = orderBook.EstimateOrderPrices(
                    side,
                    Amount,
                    FromCurrency.DigitsMultiplier,
                    baseCurrency.DigitsMultiplier);

                _estimatedMaxAmount = orderBook.EstimateMaxAmount(side, FromCurrency.DigitsMultiplier);
                EstimatedRedeemFee = ToCurrency.GetRedeemFee(walletAddress);

                _isNoLiquidity = Amount != 0 && _estimatedOrderPrice == 0;

                if (symbol.IsBaseCurrency(ToCurrency.Name))
                {
                    _targetAmount = _estimatedPrice != 0
                        ? AmountHelper.RoundDown(Amount / _estimatedPrice, ToCurrency.DigitsMultiplier)
                        : 0m;
                }
                else if (symbol.IsQuoteCurrency(ToCurrency.Name))
                {
                    _targetAmount = AmountHelper.RoundDown(Amount * _estimatedPrice, ToCurrency.DigitsMultiplier);
                }

                if (Application.Current.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        OnPropertyChanged(nameof(EstimatedPrice));
                        OnPropertyChanged(nameof(EstimatedMaxAmount));
                        OnPropertyChanged(nameof(PriceFormat));
                        OnPropertyChanged(nameof(IsNoLiquidity));

                        OnPropertyChanged(nameof(TargetCurrencyFormat));
                        OnPropertyChanged(nameof(TargetAmount));

                        UpdateTargetAmountInBase(App.QuotesProvider);
                    }, DispatcherPriority.Background);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Quotes updated event handler error");
            }
        }

        private async void OnSwapEventHandler(object sender, SwapEventArgs args)
        {
            try
            {
                var swaps = await App.Account
                    .GetSwapsAsync();

                if (Application.Current.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var swapViewModels = swaps
                            .Select(s => SwapViewModelFactory.CreateSwapViewModel(s, Currencies))
                            .ToList()
                            .SortList((s1, s2) => s2.Time.ToUniversalTime()
                                .CompareTo(s1.Time.ToUniversalTime()));

                        Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);
                    }, DispatcherPriority.Background);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Swaps update error");
            }
        }

        private void OnConvertClick()
        {
            if (Amount == 0)
            {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvWrongAmount);
                return;
            }

            if (EstimatedPrice == 0)
            {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvNoLiquidity);
                return;
            }

            if (!App.Terminal.IsServiceConnected(TerminalService.All))
            {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvServicesUnavailable);
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
            if (symbol == null)
            {
                DialogViewer.ShowMessage(Resources.CvError, Resources.CvNotSupportedSymbol);
                return;
            }

            var side = symbol.OrderSideForBuyCurrency(ToCurrency);
            var price = EstimatedPrice;
            var baseCurrency = Currencies.GetByName(symbol.Base);
            var qty = AmountHelper.AmountToQty(side, Amount, price, baseCurrency.DigitsMultiplier);

            if (qty < symbol.MinimumQty)
            {
                var minimumAmount = AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrency.DigitsMultiplier);
                var message = string.Format(CultureInfo.InvariantCulture, Resources.CvMinimumAllowedQtyWarning, minimumAmount, FromCurrency.Name);

                DialogViewer.ShowMessage(Resources.CvWarning, message);
                return;
            }

            var viewModel = new ConversionConfirmationViewModel(App, DialogViewer)
            {
                FromCurrency = FromCurrency,
                ToCurrency = ToCurrency,
                FromCurrencyViewModel = FromCurrencyViewModel,
                ToCurrencyViewModel = ToCurrencyViewModel,

                PriceFormat = PriceFormat,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat,
                TargetCurrencyCode = TargetCurrencyCode,
                TargetCurrencyFormat = TargetCurrencyFormat,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,

                FromFeeCurrencyCode = FromFeeCurrencyCode,
                FromFeeCurrencyFormat = FromFeeCurrencyFormat,
                TargetFeeCurrencyCode = TargetFeeCurrencyCode,
                TargetFeeCurrencyFormat = TargetFeeCurrencyFormat,

                Amount = Amount,
                AmountInBase = AmountInBase,
                TargetAmount = TargetAmount,
                TargetAmountInBase = TargetAmountInBase,

                EstimatedPrice = EstimatedPrice,
                EstimatedOrderPrice = _estimatedOrderPrice,
                EstimatedPaymentFee = EstimatedPaymentFee,
                EstimatedRedeemFee = EstimatedRedeemFee,
                EstimatedPaymentFeeInBase = EstimatedPaymentFeeInBase,
                EstimatedRedeemFeeInBase = EstimatedRedeemFeeInBase,

                RewardForRedeem = RewardForRedeem,
                RewardForRedeemInBase = RewardForRedeemInBase,
                HasRewardForRedeem = HasRewardForRedeem
            };

            viewModel.OnSuccess += OnSuccessConvertion;

            DialogViewer.ShowDialog(Dialogs.Convert, viewModel, defaultPageId: Pages.ConversionConfirmation);
        }

        private void OnSuccessConvertion(object sender, EventArgs e)
        {
            Amount = _amount; // recalculate amount
        }

        private void DesignerMode()
        {
            var btc = DesignTime.Currencies.Get<Bitcoin>("BTC");
            var ltc = DesignTime.Currencies.Get<Litecoin>("LTC");

            _currencyViewModels = new List<CurrencyViewModel>
            {
                 CurrencyViewModelCreator.CreateViewModel(btc, subscribeToUpdates: false),
                 CurrencyViewModelCreator.CreateViewModel(ltc, subscribeToUpdates: false)
            };

            _fromCurrencies = _currencyViewModels;
            _toCurrencies = _currencyViewModels;
            _fromCurrency = btc;
            _toCurrency = ltc;

            FromCurrencyViewModel = _currencyViewModels.FirstOrDefault(c => c.Currency.Name == _fromCurrency.Name);
            ToCurrencyViewModel = _currencyViewModels.FirstOrDefault(c => c.Currency.Name == _toCurrency.Name);

            var swapViewModels = new List<SwapViewModel>()
            {
                SwapViewModelFactory.CreateSwapViewModel(new Swap
                {
                    Symbol = "LTC/BTC",
                    Price = 0.0000888m,
                    Qty = 0.001000m,
                    Side = Side.Buy,
                    TimeStamp = DateTime.UtcNow
                },
                DesignTime.Currencies),
                SwapViewModelFactory.CreateSwapViewModel(new Swap
                {
                    Symbol = "LTC/BTC",
                    Price = 0.0100808m,
                    Qty = 0.0043000m,
                    Side = Side.Sell,
                    TimeStamp = DateTime.UtcNow
                },
                DesignTime.Currencies)
            };

            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);

            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, FromCurrency.FeeCurrencyName);
        }
    }
}