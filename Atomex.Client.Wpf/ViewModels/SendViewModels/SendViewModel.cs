using Atomex.Core.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.MarketData.Abstract;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Helpers;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class SendViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }

        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private Currency _currency;
        public Currency Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged(propertyName: nameof(Currency));

                CurrencyViewModel = FromCurrencies.FirstOrDefault(c => c.Currency.Name == Currency.Name);

                _amount = 0;
                OnPropertyChanged(propertyName: nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(propertyName: nameof(FeeString));

                _feePrice = 0;
                OnPropertyChanged(propertyName: nameof(FeePriceString));

                FeeName = CurrencyViewModel.FeeName;
                UseFeePrice = _currency.HasFeePrice;

                if (UseFeePrice)
                {
                    FeePriceFormat = _currency.FeePriceFormat;
                    FeePriceCode = _currency.FeePriceCode;
                }

                Warning = string.Empty;
            }
        }

        private CurrencyViewModel _currencyViewModel;
        private CurrencyViewModel CurrencyViewModel
        {
            get => _currencyViewModel;
            set
            {
                _currencyViewModel = value;

                CurrencyCode = _currencyViewModel?.CurrencyCode;
                FeeCurrencyCode = _currencyViewModel?.FeeCurrencyCode;
                BaseCurrencyCode = _currencyViewModel?.BaseCurrencyCode;

                CurrencyFormat = _currencyViewModel?.CurrencyFormat;
                FeeCurrencyFormat = CurrencyViewModel?.FeeCurrencyFormat;
                BaseCurrencyFormat = _currencyViewModel?.BaseCurrencyFormat;
            }
        }

        private string _to;
        public string To
        {
            get => _to;
            set
            {
                _to = value;
                OnPropertyChanged(nameof(To));
                Warning = string.Empty;
            }
        }

        private string CurrencyFormat { get; set; }
        private string FeeCurrencyFormat { get; set; }
        private string FeePriceFormat { get; set; }

        private string _baseCurrencyFormat;
        public string BaseCurrencyFormat
        {
            get => _baseCurrencyFormat;
            set { _baseCurrencyFormat = value; OnPropertyChanged(nameof(BaseCurrencyFormat)); }
        }

        private decimal _amount;
        private decimal Amount
        {
            get => _amount;
            set
            {
                var previousAmount = _amount;
                _amount = value;

                if (UseDefaultFee)
                {
                    var estimatedFee = _amount != 0
                        ? (_amount < CurrencyViewModel.AvailableAmount
                            ? App.Account
                                .EstimateFeeAsync(Currency, To, _amount, BlockchainTransactionType.Output)
                                .WaitForResult()
                            : null)
                        : 0;

                    if (estimatedFee == null)
                    {
                        var (maxAmount, maxFee) = App.Account
                            .EstimateMaxAmountToSendAsync(Currency, To, BlockchainTransactionType.Output)
                            .WaitForResult();

                        if (maxAmount > 0)
                        {
                            _amount = maxAmount;
                            estimatedFee = maxFee;
                        }
                        else
                        {
                            _amount = previousAmount;
                            Warning = Resources.CvInsufficientFunds;
                            return;
                        }
                    } 

                    if (_amount + estimatedFee.Value > CurrencyViewModel.AvailableAmount)
                        _amount = Math.Max(CurrencyViewModel.AvailableAmount - estimatedFee.Value, 0);

                    if (_amount == 0)
                        estimatedFee = 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(estimatedFee.Value, Currency.GetDefaultFeePrice());
                    OnPropertyChanged(nameof(FeeString));

                    if (UseFeePrice)
                    {
                        _feePrice = Currency.GetDefaultFeePrice();
                        OnPropertyChanged(nameof(FeePriceString));
                    }

                    Warning = string.Empty;
                }
                else
                {
                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                    if (_amount + feeAmount > CurrencyViewModel.AvailableAmount)
                        _amount = Math.Max(CurrencyViewModel.AvailableAmount - feeAmount, 0);

                    OnPropertyChanged(nameof(AmountString));

                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        public string AmountString
        {
            get => Amount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    return;

                Amount = amount.TruncateByFormat(CurrencyFormat);
            }
        }

        private decimal _fee;
        private decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                    if (_amount + feeAmount > CurrencyViewModel.AvailableAmount)
                        feeAmount = Math.Max(CurrencyViewModel.AvailableAmount - _amount, 0);

                    _fee = Currency.GetFeeFromFeeAmount(feeAmount, _feePrice);

                    OnPropertyChanged(nameof(FeeString));
                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        public string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(FeeCurrencyFormat);
            }
        }

        private decimal _feePrice;
        private decimal FeePrice
        {
            get => _feePrice;
            set
            {
                _feePrice = value;

                if (!UseDefaultFee)
                {
                    var feeAmount = Currency.GetFeeAmount(_fee, _feePrice);

                    if (_amount + feeAmount > CurrencyViewModel.AvailableAmount)
                        feeAmount = Math.Max(CurrencyViewModel.AvailableAmount - _amount, 0);

                    _feePrice = Currency.GetFeePriceFromFeeAmount(feeAmount, _fee);

                    OnPropertyChanged(nameof(FeePriceString));
                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
        }

        public string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gasPrice))
                    return;

                FeePrice = gasPrice.TruncateByFormat(FeePriceFormat);
            }
        }

        private bool _useFeePrice;
        public bool UseFeePrice
        {
            get => _useFeePrice;
            set { _useFeePrice = value; OnPropertyChanged(nameof(UseFeePrice)); }
        }

        private bool _useDefaultFee;
        public bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                    Amount = _amount; // recalculate amount and fee using default fee
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

        private string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        private string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        private string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        }

        private string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private string _feeName;
        public string FeeName
        {
            get => _feeName;
            set { _feeName = value; OnPropertyChanged(nameof(FeeName)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer?.HideSendDialog();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(() =>
        {
            if (string.IsNullOrEmpty(To)) {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!Currency.IsValidAddress(To)) {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Amount <= 0) {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (Fee < 0) {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            if (Amount + Currency.GetFeeAmount(Fee, FeePrice) > CurrencyViewModel.AvailableAmount) {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel(DialogViewer)
            {
                Currency = Currency,
                To = To,
                Amount = Amount,
                AmountInBase = AmountInBase,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee = Fee,
                FeeInBase = FeeInBase,
                FeePrice = FeePrice,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat
            };

            Navigation.Navigate(
                uri: Navigation.SendConfirmationAlias,
                context: confirmationViewModel);
        }));

        public SendViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public SendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            FromCurrencies = App.Account.Currencies
                .Where(c => c.IsTransactionsAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            Currency = currency;

            SubscribeToServices();
        }

        public void Show()
        {
            Navigation.Navigate(
                uri: Navigation.SendAlias,
                context: this);
        }

        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        private void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }

        private void DesignerMode()
        {
            FromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            _currency = FromCurrencies[0].Currency;
            _to = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            _amount = 0.00001234m;
            _amountInBase = 10.23m;
            _fee = 0.0001m;
            _feeInBase = 8.43m;
        }
    }
}