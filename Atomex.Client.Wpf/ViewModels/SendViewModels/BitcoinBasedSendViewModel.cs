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
using Atomex.Common;
using Atomex.Core.Entities;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : BaseViewModel
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

                if (UseDefaultFee)
                {
                    var account = App.Account
                        .GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                    var estimatedFee = _amount != 0
                        ? (_amount < CurrencyViewModel.AvailableAmount
                            ? account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
                                .WaitForResult()
                            : null)
                        : 0;

                    if (estimatedFee == null)
                    {
                        var (maxAmount, maxFee) = account
                            .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output)
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

                    _fee = estimatedFee.Value;
                    OnPropertyChanged(nameof(FeeString));

                    Warning = string.Empty;
                }
                else
                {
                    if (_amount + _fee > CurrencyViewModel.AvailableAmount)
                        _amount = Math.Max(CurrencyViewModel.AvailableAmount - _fee, 0);

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
        public decimal Fee
        {
            get => _fee;
            set
            {
                _fee = value;

                if (!UseDefaultFee)
                {
                    if (_amount + _fee > CurrencyViewModel.AvailableAmount)
                        _fee = Math.Max(CurrencyViewModel.AvailableAmount - _amount, 0);

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

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer.HideDialog(Dialogs.BitcoinBasedSend);
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

            if (Amount + Fee > CurrencyViewModel.AvailableAmount) {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel(DialogViewer, Dialogs.BitcoinBasedSend)
            {
                Currency = Currency,
                To = To,
                Amount = Amount,
                AmountInBase = AmountInBase,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee = Fee,
                FeeInBase = FeeInBase,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat
            };

            DialogViewer.PushPage(Dialogs.BitcoinBasedSend, Pages.SendConfirmation, confirmationViewModel);
        }));

        public BitcoinBasedSendViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public BitcoinBasedSendViewModel(
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