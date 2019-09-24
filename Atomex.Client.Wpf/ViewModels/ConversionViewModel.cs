using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Abstract;
using Atomex.Blockchain.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.Core.Entities;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using Atomex.Swaps;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Helpers;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ConversionViewModel : BaseViewModel, IConversionViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }

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

                var oldToCurrency = ToCurrency;

                ToCurrencies = _currencyViewModels
                    .Where(c => Symbols.SymbolByCurrencies(c.Currency, _fromCurrency) != null)
                    .ToList();

                if (oldToCurrency != null &&
                    oldToCurrency != _fromCurrency &&
                    ToCurrencies.FirstOrDefault(c => c.Currency.Name == oldToCurrency.Name) != null)
                {
                    ToCurrency = oldToCurrency;
                }
                else
                {
                    ToCurrency = ToCurrencies.First().Currency;
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

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set
            {
                var previousAmount = _amount;
                _amount = value;

                var estimatedPaymentFee = _amount != 0
                    ? (_amount < FromCurrencyViewModel.AvailableAmount
                        ? App.Account
                            .EstimateFeeAsync(FromCurrency, null, _amount, BlockchainTransactionType.SwapPayment)
                            .WaitForResult()
                        : null)
                    : 0;

                if (estimatedPaymentFee == null)
                {
                    var (maxAmount, maxFee) = App.Account
                        .EstimateMaxAmountToSendAsync(FromCurrency, null, BlockchainTransactionType.SwapPayment)
                        .WaitForResult();

                    if (maxAmount > 0)
                    {
                        _amount = maxAmount;
                        estimatedPaymentFee = maxFee;
                    }
                    else
                    {
                        _amount = previousAmount;
                        // todo: insufficient funds warning
                        return;
                    }
                }

                _estimatedPaymentFee = estimatedPaymentFee.Value;
                _estimatedRedeemFee = ToCurrency.GetDefaultRedeemFee();
                _useRewardForRedeem = false;

                if (ToCurrencyViewModel.AvailableAmount == 0 && !(ToCurrency is BitcoinBasedCurrency))
                {
                    _estimatedRedeemFee *= 2; // todo: show hint or tool tip
                    _useRewardForRedeem = true;
                }

                if (_amount + _estimatedPaymentFee > FromCurrencyViewModel.AvailableAmount)
                    _amount = Math.Max(FromCurrencyViewModel.AvailableAmount - _estimatedPaymentFee, 0);

                OnPropertyChanged(nameof(CurrencyFormat));
                OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(Amount));
                OnPropertyChanged(nameof(EstimatedPaymentFee));
                OnPropertyChanged(nameof(EstimatedRedeemFee));
                
#if DEBUG
                if (!Env.IsInDesignerMode()) {
#endif
                    OnQuotesUpdatedEventHandler(App.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
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

        private decimal _estimatedMaxAmount;
        public decimal EstimatedMaxAmount
        {
            get => _estimatedMaxAmount;
            set { _estimatedMaxAmount = value; OnPropertyChanged(nameof(EstimatedMaxAmount)); }
        }

        private decimal _estimatedPaymentFee;
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

        private bool _useRewardForRedeem;

        private decimal _estimatedRedeemFeeInBase;
        public decimal EstimatedRedeemFeeInBase
        {
            get => _estimatedRedeemFeeInBase;
            set { _estimatedRedeemFeeInBase = value; OnPropertyChanged(nameof(EstimatedRedeemFeeInBase)); }
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
            App.AccountChanged += OnAccountChangedEventHandler;

            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnBaseQuotesUpdatedEventHandler;

            App.Terminal.QuotesUpdated += OnQuotesUpdatedEventHandler;
            App.Terminal.SwapUpdated += OnSwapEventHandler;
        }

        private void OnAccountChangedEventHandler(object sender, AccountChangedEventArgs args)
        {
            if (!(sender is AtomexApp))
                return;

            if (args.NewAccount == null)
                return;

            var account = args.NewAccount;

            _currencyViewModels = account.Currencies
                .Where(c => c.IsSwapAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            FromCurrencies = _currencyViewModels.ToList();
            FromCurrency = account.Currencies.Get<Bitcoin>();
            ToCurrency = account.Currencies.Get<Litecoin>();

            OnSwapEventHandler(this, null);
        }

        private void OnBaseQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider provider))
                return;

            if (CurrencyCode == null || TargetCurrencyCode == null || BaseCurrencyCode == null)
                return;

            var fromCurrencyPrice = provider.GetQuote(CurrencyCode, BaseCurrencyCode)?.Bid ?? 0m;
            AmountInBase = _amount * fromCurrencyPrice;
            EstimatedPaymentFeeInBase = _estimatedPaymentFee * fromCurrencyPrice;

            var toCurrencyPrice = provider.GetQuote(TargetCurrencyCode, BaseCurrencyCode)?.Bid ?? 0m;
            EstimatedRedeemFeeInBase = _estimatedRedeemFee * toCurrencyPrice;

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

        private async void OnQuotesUpdatedEventHandler(object sender, MarketDataEventArgs args)
        {
            try
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

                if (orderBook == null)
                    return;

                _estimatedPrice = orderBook.EstimatedDealPrice(side, Amount);
                _estimatedMaxAmount = orderBook.EstimateMaxAmount(side);

                _isNoLiquidity = Amount != 0 && _estimatedPrice == 0;

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
                            .Select(SwapViewModelFactory.CreateSwapViewModel)
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
            if (Amount == 0) {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvWrongAmount);
                return;
            }

            if (!App.Terminal.IsServiceConnected(TerminalService.All)) {
                DialogViewer.ShowMessage(Resources.CvWarning, Resources.CvServicesUnavailable);
                return;
            }

            var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
            if (symbol == null) {
                DialogViewer.ShowMessage(Resources.CvError, Resources.CvNotSupportedSymbol);
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

                Amount = Amount,
                AmountInBase = AmountInBase,
                TargetAmount = TargetAmount,
                TargetAmountInBase = TargetAmountInBase,

                EstimatedPrice = EstimatedPrice,
                EstimatedPaymentFee = EstimatedPaymentFee,
                EstimatedRedeemFee = EstimatedRedeemFee,
                EstimatedPaymentFeeInBase = EstimatedPaymentFeeInBase,
                EstimatedRedeemFeeInBase = EstimatedRedeemFeeInBase,

                UseRewardForRedeem = _useRewardForRedeem
            };

            DialogViewer?.ShowConversionConfirmationDialog(viewModel, dialogLoaded: () => {
                viewModel.Show();
            });
        }

        private void DesignerMode()
        {
            _currencyViewModels = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            FromCurrencies = _currencyViewModels.ToList();
            FromCurrency = DesignTime.Currencies.Get<Bitcoin>();
            ToCurrency = DesignTime.Currencies.Get<Litecoin>();

            var symbol = DesignTime.Symbols.GetByName("LTC/BTC");

            var swapViewModels = new List<SwapViewModel>()
            {
                SwapViewModelFactory.CreateSwapViewModel(new ClientSwap
                {
                    Symbol = symbol,
                    Price = 0.0000888m,
                    Qty = 0.001000m,
                    Side = Side.Buy,
                    TimeStamp = DateTime.UtcNow
                }),
                SwapViewModelFactory.CreateSwapViewModel(new ClientSwap
                {

                    Symbol = symbol,
                    Price = 0.0100808m,
                    Qty = 0.0043000m,
                    Side = Side.Sell,
                    TimeStamp = DateTime.UtcNow
                })
            };

            Swaps = new ObservableCollection<SwapViewModel>(swapViewModels);
        }
    }
}