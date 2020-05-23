using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class SendViewModel : BaseViewModel
    {
        protected IAtomexApp App { get; set; }
        protected IDialogViewer DialogViewer { get; set; }

        private List<CurrencyViewModel> _fromCurrencies;
        public virtual List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        protected Currency _currency;
        public virtual Currency Currency
        {
            get => _currency;
            set
            {
                if (_currency != null && _currency != value)
                {
                    DialogViewer.HideDialog(Dialogs.Send);

                    var sendViewModel = SendViewModelCreator.CreateViewModel(App, DialogViewer, value);
                    var sendPageId = SendViewModelCreator.GetSendPageId(value);

                    DialogViewer.ShowDialog(Dialogs.Send, sendViewModel, defaultPageId: sendPageId);
                    return;
                }

                _currency = value;
                OnPropertyChanged(nameof(Currency));

                CurrencyViewModel = FromCurrencies.FirstOrDefault(c => c.Currency.Name == Currency.Name);

                _amount = 0;
                OnPropertyChanged(nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(nameof(FeeString));

                Warning = string.Empty;
            }
        }

        protected CurrencyViewModel _currencyViewModel;
        public virtual CurrencyViewModel CurrencyViewModel
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

        protected string _to;
        public virtual string To
        {
            get => _to;
            set 
            {
                _to = value;
                OnPropertyChanged(nameof(To));
                Warning = string.Empty;
            }
        }

        protected string CurrencyFormat { get; set; }
        protected string FeeCurrencyFormat { get; set; }

        private string _baseCurrencyFormat;
        public virtual string BaseCurrencyFormat
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

        private bool _isAmountUpdating;
        public bool IsAmountUpdating
        {
            get => _isAmountUpdating;
            set { _isAmountUpdating = value; OnPropertyChanged(nameof(IsAmountUpdating)); }
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

        private bool _isFeeUpdating;
        public bool IsFeeUpdating
        {
            get => _isFeeUpdating;
            set { _isFeeUpdating = value; OnPropertyChanged(nameof(IsFeeUpdating)); }
        }

        protected decimal _fee;
        public decimal Fee
        {
            get => _fee;
            set { UpdateFee(value); }
        }

        public virtual string FeeString
        {
            get => Fee.ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(FeeCurrencyFormat);
            }
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

        protected decimal _amountInBase;
        public decimal AmountInBase
        {
            get => _amountInBase;
            set { _amountInBase = value; OnPropertyChanged(nameof(AmountInBase)); }
        }

        protected decimal _feeInBase;
        public decimal FeeInBase
        {
            get => _feeInBase;
            set { _feeInBase = value; OnPropertyChanged(nameof(FeeInBase)); }
        }

        protected string _currencyCode;
        public string CurrencyCode
        {
            get => _currencyCode;
            set { _currencyCode = value; OnPropertyChanged(nameof(CurrencyCode)); }
        }

        protected string _feeCurrencyCode;
        public string FeeCurrencyCode
        {
            get => _feeCurrencyCode;
            set { _feeCurrencyCode = value; OnPropertyChanged(nameof(FeeCurrencyCode)); }
        }

        protected string _baseCurrencyCode;
        public string BaseCurrencyCode
        {
            get => _baseCurrencyCode;
            set { _baseCurrencyCode = value; OnPropertyChanged(nameof(BaseCurrencyCode)); }
        }

        protected string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer.HideDialog(Dialogs.Send);
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(OnNextCommand));

        protected virtual void OnNextCommand()
        {
            if (string.IsNullOrEmpty(To))
            {
                Warning = Resources.SvEmptyAddressError;
                return;
            }

            if (!Currency.IsValidAddress(To))
            {
                Warning = Resources.SvInvalidAddressError;
                return;
            }

            if (Amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (Fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            if (Amount + feeAmount > CurrencyViewModel.AvailableAmount)
            {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel(DialogViewer, Dialogs.Send)
            {
                Currency = Currency,
                To = To,
                Amount = Amount,
                AmountInBase = AmountInBase,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee = Fee,
                UseDeafultFee = UseDefaultFee,
                FeeInBase = FeeInBase,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat,

                FeeCurrencyCode = FeeCurrencyCode,
                FeeCurrencyFormat = FeeCurrencyFormat
            };

            DialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
        }

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

            Currency = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == currency.Name)
                .Currency;

            UseDefaultFee = true; // use default fee by default

            SubscribeToServices();
        }
        private void SubscribeToServices()
        {
            if (App.HasQuotesProvider)
                App.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        protected virtual async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            var previousAmount = _amount;
            _amount = amount;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? (_amount < availableAmount
                            ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                            : null)
                        : 0;

                    if (estimatedFeeAmount == null)
                    {
                        if (maxAmount > 0)
                        {
                            _amount = maxAmount;
                            estimatedFeeAmount = maxFeeAmount;
                        }
                        else
                        {
                            _amount = previousAmount;
                            Warning = Resources.CvInsufficientFunds;
                            return;
                        }
                    }

                    if (_amount + estimatedFeeAmount.Value > availableAmount)
                        _amount = Math.Max(availableAmount - estimatedFeeAmount.Value, 0);

                    if (_amount == 0)
                        estimatedFeeAmount = 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount.Value, Currency.GetDefaultFeePrice());
                    OnPropertyChanged(nameof(FeeString));

                    Warning = string.Empty;
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, false);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Math.Max(Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice()), maxFeeAmount);

                    if (_amount + feeAmount > availableAmount)
                        _amount = Math.Max(availableAmount - feeAmount, 0);

                    OnPropertyChanged(nameof(AmountString));

                    if (_fee != 0)
                        Fee = _fee;

                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual async void UpdateFee(decimal fee)
        {
            if (_amount == 0)
            {
                _fee = 0;
                return;
            }

            try
            {
                IsFeeUpdating = true;

                _fee = Math.Min(fee, Currency.GetMaximumFee());

                if (!UseDefaultFee)
                {
                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    var feeAmount = _fee;

                    if (feeAmount > estimatedFeeAmount.Value)
                    {
                        var (maxAmount, maxFee, _) = await App.Account
                            .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                        var availableAmount = Currency is BitcoinBasedCurrency
                            ? CurrencyViewModel.AvailableAmount
                            : maxAmount + maxFee;

                        if (_amount + feeAmount > availableAmount)
                            _amount = Math.Max(availableAmount - feeAmount, 0);
                    }
                    else if (feeAmount < estimatedFeeAmount.Value)
                        _fee = estimatedFeeAmount.Value;

                    if (_amount == 0)
                        _fee = 0;

                    OnPropertyChanged(nameof(AmountString));

                    OnPropertyChanged(nameof(FeeString));
                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (quote?.Bid ?? 0m);
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