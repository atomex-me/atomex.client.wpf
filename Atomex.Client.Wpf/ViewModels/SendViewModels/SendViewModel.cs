using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Atomex.Common;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class SendViewModel : BaseViewModel
    {
        protected IAtomexApp _app;
        protected IDialogViewer _dialogViewer;

        private List<CurrencyViewModel> _fromCurrencies;
        public virtual List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        protected CurrencyConfig _currency;
        public virtual CurrencyConfig Currency
        {
            get => _currency;
            set
            {
                if (_currency != null && _currency != value)
                {
                    _dialogViewer.HideDialog(Dialogs.Send);

                    var sendViewModel = SendViewModelCreator.CreateViewModel(_app, _dialogViewer, value);
                    var sendPageId = SendViewModelCreator.GetSendPageId(value);

                    _dialogViewer.ShowDialog(Dialogs.Send, sendViewModel, defaultPageId: sendPageId);
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

                CurrencyCode     = _currencyViewModel?.CurrencyCode;
                FeeCurrencyCode  = _currencyViewModel?.FeeCurrencyCode;
                BaseCurrencyCode = _currencyViewModel?.BaseCurrencyCode;

                CurrencyFormat     = _currencyViewModel?.CurrencyFormat;
                FeeCurrencyFormat  = CurrencyViewModel?.FeeCurrencyFormat;
                BaseCurrencyFormat = _currencyViewModel?.BaseCurrencyFormat;
            }
        }

        private ObservableCollection<WalletAddressViewModel> _fromAddresses;
        public ObservableCollection<WalletAddressViewModel> FromAddresses
        {
            get => _fromAddresses;
            set
            {
                _fromAddresses = value;
                OnPropertyChanged(nameof(FromAddresses));
            }
        }

        private string _from;
        public string From
        {
            get => _from;
            set
            {
                _from = value;
                OnPropertyChanged(nameof(From));

                Warning = string.Empty;
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
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(FeeCurrencyFormat);
            }
        }

        protected bool _useDefaultFee;
        public virtual bool UseDefaultFee
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
        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            _dialogViewer.HideDialog(Dialogs.Send);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(OnNextCommand);

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

            if (_amount <= 0)
            {
                Warning = Resources.SvAmountLessThanZeroError;
                return;
            }

            if (_fee <= 0)
            {
                Warning = Resources.SvCommissionLessThanZeroError;
                return;
            }

            if (string.IsNullOrEmpty(From))
            {
                Warning = "Invalid 'From' address!";
                return;
            }

            var isToken = Currency.FeeCurrencyName != Currency.Name;

            var feeAmount = !isToken ? Fee : 0;

            var fromAddress = _app.Account
                .GetAddressAsync(Currency.Name, From)
                .WaitForResult();

            if (fromAddress.AvailableBalance() < _amount + feeAmount)
            {
                Warning = Resources.SvAvailableFundsError;
                return;
            }

            var confirmationViewModel = new SendConfirmationViewModel(_dialogViewer, Dialogs.Send)
            {
                Currency           = Currency,
                From               = _from,
                To                 = _to,
                Amount             = _amount,
                AmountInBase       = _amountInBase,
                BaseCurrencyCode   = _baseCurrencyCode,
                BaseCurrencyFormat = _baseCurrencyFormat,
                Fee                = _fee,
                UseDeafultFee      = _useDefaultFee,
                FeeInBase          = _feeInBase,
                CurrencyCode       = _currencyCode,
                CurrencyFormat     = CurrencyFormat,
                FeeCurrencyCode    = _feeCurrencyCode,
                FeeCurrencyFormat  = FeeCurrencyFormat
            };

            _dialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
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
            CurrencyConfig currency)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            FromCurrencies = _app.Account.Currencies
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            Currency = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == currency.Name)
                .Currency;

            FromAddresses = new ObservableCollection<WalletAddressViewModel>(GetFromAddressList());
            From = FromAddresses.MaxByOrDefault(w => w.AvailableBalance)?.Address;

            UseDefaultFee = true; // use default fee by default

            SubscribeToServices();
        }
        private void SubscribeToServices()
        {
            if (_app.HasQuotesProvider)
                _app.QuotesProvider.QuotesUpdated += OnQuotesUpdatedEventHandler;
        }

        protected virtual async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;
            
            _amount = amount;

            try
            {
                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                var account = _app.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            from: _from,
                            to: _to,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: true);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(
                            from: _from,
                            to: _to,
                            amount: _amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            from: _from,
                            to: _to,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    var availableAmount = Currency is BitcoinBasedConfig
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (_amount > maxAmount || _amount + feeAmount > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            Warning = string.Empty;

            try
            {
                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, defaultFeePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;

                    return;
                }

                if (!UseDefaultFee)
                {
                    var account = _app.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(
                            from: _from,
                            to: _to,
                            amount: _amount,
                            type: BlockchainTransactionType.Output)
                        : 0;

                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            from: _from,
                            to: _to,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    var availableAmount = Currency is BitcoinBasedConfig
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (_amount + feeAmount > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }
                    else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                    {
                        Warning = Resources.CvLowFees;
                    }

                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected ICommand _maxCommand;
        public ICommand MaxCommand => _maxCommand ??= new Command(OnMaxClick);

        protected virtual async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                var defaultFeePrice = await Currency
                    .GetDefaultFeePriceAsync();

                var account = _app.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            from: _from,
                            to: _to,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: true);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(
                            from: _from,
                            to: _to,
                            type: BlockchainTransactionType.Output,
                            fee: 0,
                            feePrice: 0,
                            reserve: false);

                    var availableAmount = Currency is BitcoinBasedConfig
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (availableAmount - feeAmount > 0)
                    {
                        _amount = availableAmount - feeAmount;

                        var estimatedFeeAmount = _amount != 0
                            ? await account.EstimateFeeAsync(
                                from: _from,
                                to: _to,
                                amount: _amount,
                                type: BlockchainTransactionType.Output)
                            : 0;

                        if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                        {
                            Warning = Resources.CvLowFees;

                            if (_fee == 0)
                            {
                                _amount = 0;
                                OnPropertyChanged(nameof(AmountString));
                                return;
                            }
                        }
                    }
                    else
                    {
                        _amount = 0;

                        Warning = Resources.CvInsufficientFunds;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (sender is not ICurrencyQuotesProvider quotesProvider)
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase    = Fee * (quote?.Bid ?? 0m);
        }

        private IEnumerable<WalletAddressViewModel> GetFromAddressList()
        {
            var addresses = _app.Account
                .GetUnspentAddressesAsync(Currency.Name)
                .WaitForResult();

            return addresses
                .Where(w => w.Balance != 0)
                .Select(w => new WalletAddressViewModel
                {
                    Address          = w.Address,
                    AvailableBalance = w.AvailableBalance(),
                    CurrencyFormat   = CurrencyFormat,
                    CurrencyCode     = CurrencyCode,
                })
                .ToList();
        }

        private void DesignerMode()
        {
            FromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            _currency     = FromCurrencies[0].Currency;
            _to           = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            _amount       = 0.00001234m;
            _amountInBase = 10.23m;
            _fee          = 0.0001m;
            _feeInBase    = 8.43m;
        }
    }
}