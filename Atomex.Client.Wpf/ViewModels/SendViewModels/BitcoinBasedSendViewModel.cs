using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.BitcoinBased;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly IDialogViewer _dialogViewer;

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
                OnPropertyChanged(nameof(Currency));

                _amount = 0;
                OnPropertyChanged(nameof(AmountString));

                _fee = 0;
                OnPropertyChanged(nameof(FeeString));

                Warning = string.Empty;
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

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set
            {
                //var previousAmount = _amount;

                //_amount = value;

                var account = _app.Account
                    .GetCurrencyAccount<BitcoinBasedAccount>(Currency.Name);

                var balance = account.GetBalance();

                if (UseDefaultFee)
                {

                }

                //if (UseDefaultFee)
                //{
                //    //var estimatedFee = _amount != 0
                //    //    ? (_amount < CurrencyViewModel.AvailableAmount
                //    //        ? account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
                //    //            .WaitForResult()
                //    //        : null)
                //    //    : 0;
                //    var estimatedFee = new decimal?(1);

                //    if (estimatedFee == null)
                //    {
                //        var (maxAmount, maxFee) = account
                //            .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output)
                //            .WaitForResult();

                //        if (maxAmount > 0)
                //        {
                //            _amount = maxAmount;
                //            estimatedFee = maxFee;
                //        }
                //        else
                //        {
                //            _amount = previousAmount;
                //            Warning = Resources.CvInsufficientFunds;
                //            return;
                //        }
                //    } 

                //    //if (_amount + estimatedFee.Value > CurrencyViewModel.AvailableAmount)
                //    //    _amount = Math.Max(CurrencyViewModel.AvailableAmount - estimatedFee.Value, 0);

                //    if (_amount == 0)
                //        estimatedFee = 0;

                //    OnPropertyChanged(nameof(AmountString));

                //    _fee = estimatedFee.Value;
                //    OnPropertyChanged(nameof(FeeString));

                //    Warning = string.Empty;
                //}
                //else
                //{
                //    //if (_amount + _fee > CurrencyViewModel.AvailableAmount)
                //    //    _amount = Math.Max(CurrencyViewModel.AvailableAmount - _fee, 0);

                //    OnPropertyChanged(nameof(AmountString));

                //    Warning = string.Empty;
                //}

                //OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
        }

        public string AmountString
        {
            get => Amount.ToString(Currency.Format, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                    return;

                Amount = amount.TruncateByFormat(Currency.Format);
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
                    //if (_amount + _fee > CurrencyViewModel.AvailableAmount)
                    //    _fee = Math.Max(CurrencyViewModel.AvailableAmount - _amount, 0);

                    OnPropertyChanged(nameof(FeeString));
                    Warning = string.Empty;
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
        }

        public string FeeString
        {
            get => Fee.ToString(Currency.FeeFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(Currency.FeeFormat);
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

        public string CurrencyCode => Currency.Name;
        public string FeeCurrencyCode => Currency.Name;
        public string BaseCurrencyCode => "USD";
        public string BaseCurrencyFormat => "$0.00";

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            _dialogViewer.HideDialog(Dialogs.BitcoinBasedSend);
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

            //if (Amount + Fee > CurrencyViewModel.AvailableAmount) {
            //    Warning = Resources.SvAvailableFundsError;
            //    return;
            //}

            var confirmationViewModel = new SendConfirmationViewModel(_dialogViewer, Dialogs.BitcoinBasedSend)
            {
                Currency = Currency,
                To = To,
                Amount = Amount,
                AmountInBase = AmountInBase,
                BaseCurrencyCode = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee = Fee,
                FeeInBase = FeeInBase,
                CurrencyCode = Currency.Name,
                CurrencyFormat = Currency.Format
            };

            _dialogViewer.PushPage(Dialogs.BitcoinBasedSend, Pages.SendConfirmation, confirmationViewModel);
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
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            FromCurrencies = _app.Account.Currencies
                .Where(c => c.IsTransactionsAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            Currency = currency;

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
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