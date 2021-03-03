using System;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.MarketData.Abstract;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public override Currency Currency
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

                _feePrice = 0;
                OnPropertyChanged(nameof(FeePriceString));

                OnPropertyChanged(nameof(TotalFeeString));

                FeePriceFormat = _currency.FeePriceFormat;
                FeePriceCode = _currency.FeePriceCode;

                Warning = string.Empty;
            }
        }

        protected string FeePriceFormat { get; set; }
        public virtual string TotalFeeCurrencyCode => CurrencyCode;

        public virtual string GasCode => "GAS";
        public virtual string GasFormat => "F0";

        public virtual string GasString
        {
            get => Fee.ToString(GasFormat, CultureInfo.InvariantCulture);
            set 
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var fee))
                    return;

                Fee = fee.TruncateByFormat(GasFormat);
            }
        }
        
        public override string To
        {
            get => _to;
            set 
            {
                _to = value;
                OnPropertyChanged(nameof(To));
                Warning = string.Empty;
                UpdateMaxAmount();
                UpdateSafeMaxAmount();
            }
        }
        
        public override decimal Fee
        {
            get => _fee;
            set
            {
                UpdateFee(value);
                UpdateMaxAmount();
                UpdateSafeMaxAmount();
            }
        }

        protected decimal _feePrice;
        public virtual decimal FeePrice
        {
            get => _feePrice;
            set
            {
                UpdateFeePrice(value);
                UpdateMaxAmount();
                UpdateSafeMaxAmount();
            }
        }

        private bool _isFeePriceUpdating;
        public bool IsFeePriceUpdating
        {
            get => _isFeePriceUpdating;
            set { _isFeePriceUpdating = value; OnPropertyChanged(nameof(IsFeePriceUpdating)); }
        }

        public virtual string FeePriceString
        {
            get => FeePrice.ToString(FeePriceFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var gasPrice))
                    return;

                FeePrice = gasPrice.TruncateByFormat(FeePriceFormat);
            }
        }

        protected decimal _totalFee;

        private bool _isTotalFeeUpdating;
        public bool IsTotalFeeUpdating
        {
            get => _isTotalFeeUpdating;
            set { _isTotalFeeUpdating = value; OnPropertyChanged(nameof(IsTotalFeeUpdating)); }
        }

        public virtual string TotalFeeString
        {
            get => _totalFee
                .ToString(FeeCurrencyFormat, CultureInfo.InvariantCulture);
            set { UpdateTotalFeeString(); }
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                if (_useDefaultFee)
                {
                    Amount = _amount; // recalculate amount and fee using default fee
                    UpdateMaxAmount();
                    UpdateSafeMaxAmount();
                }
            }
        }

        protected string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        }
        
        protected decimal _maxAmount;
        public decimal MaxAmount
        {
            get => _maxAmount;
            set { _maxAmount = value; OnPropertyChanged(nameof(MaxAmount)); }
        }

        private bool _isMaxAmountUpdating;
        public bool IsMaxAmountUpdating
        {
            get => _isMaxAmountUpdating;
            set { _isMaxAmountUpdating = value; OnPropertyChanged(nameof(IsMaxAmountUpdating)); }
        }

        public string MaxAmountString
        {
            get => MaxAmount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var maxAmount))
                    return;

                MaxAmount = maxAmount.TruncateByFormat(CurrencyFormat);
            }
        }
        
        protected decimal _safeMaxAmount;
        public decimal SafeMaxAmount
        {
            get => _safeMaxAmount;
            set { _safeMaxAmount = value; OnPropertyChanged(nameof(SafeMaxAmount)); }
        }

        private bool _isSafeMaxAmountUpdating;
        public bool IsSafeMaxAmountUpdating
        {
            get => _isSafeMaxAmountUpdating;
            set { _isSafeMaxAmountUpdating = value; OnPropertyChanged(nameof(IsSafeMaxAmountUpdating)); }
        }

        public string SafeMaxAmountString
        {
            get => SafeMaxAmount.ToString(CurrencyFormat, CultureInfo.InvariantCulture) + ",";
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var safeAmount))
                    return;

                SafeMaxAmount = safeAmount.TruncateByFormat(CurrencyFormat);
            }
        }

        private ICommand _safeMaxCommand;
        public ICommand SafeMaxCommand => _safeMaxCommand ??= new Command(OnSafeMaxClick);


        public EthereumSendViewModel()
            : base()
        {
        }

        public EthereumSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer, currency)
        {
        }

        protected override async void UpdateAmount(decimal amount)
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            _amount = amount;

            Warning = string.Empty;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                    _fee = Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    OnPropertyChanged(nameof(FeePriceString));

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0) 
                        Warning = Resources.CvLowFees;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected async void UpdateFeePrice(decimal value)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _feePrice = value;

            Warning = string.Empty;

            try
            {
                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }

                if (value == 0)
                {
                    Warning = Resources.CvLowFees;
                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                    return;
                }

                if (!UseDefaultFee)
                {
                    var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    OnPropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected override async void UpdateFee(decimal fee)
        {
            if (IsFeeUpdating)
                return;

            IsFeeUpdating = true;

            _fee = Math.Min(fee, Currency.GetMaximumFee());

            Warning = string.Empty;

            try
            {
                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, _feePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }

                if (_fee < Currency.GetDefaultFee())
                {
                    Warning = Resources.CvLowFees;
                    if (fee == 0)
                    {
                        UpdateTotalFeeString();
                        OnPropertyChanged(nameof(TotalFeeString));
                        return;
                    }
                }

                if (!UseDefaultFee)
                {
                    var (maxAmount, maxFee, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));

                    OnPropertyChanged(nameof(GasString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
            }
        }

        protected async void UpdateTotalFeeString(decimal totalFeeAmount = 0)
        {
            IsTotalFeeUpdating = true;

            try
            {
                var feeAmount = totalFeeAmount > 0
                    ? totalFeeAmount
                    : Currency.GetFeeAmount(_fee, _feePrice) > 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output, _fee, _feePrice)
                        : 0;

                if (feeAmount != null)
                    _totalFee = feeAmount.Value;
            }
            finally
            {
                IsTotalFeeUpdating = false;
            }
        }


        protected override void OnNextCommand()
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

            var feeAmount = !isToken ? Currency.GetFeeAmount(Fee, FeePrice) : 0;

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
                FeePrice = FeePrice,
                CurrencyCode = CurrencyCode,
                CurrencyFormat = CurrencyFormat,

                FeeCurrencyCode = FeeCurrencyCode,
                FeeCurrencyFormat = FeeCurrencyFormat
            };

            DialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }


        private async void UpdateMaxAmount()
        {
            if (IsMaxAmountUpdating)
                return;

            IsMaxAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                    if (maxAmount > 0)
                        _maxAmount = maxAmount;

                    OnPropertyChanged(nameof(MaxAmountString));

                    _fee = Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    OnPropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                    {
                        Warning = Resources.CvLowFees;
                        if (_fee == 0 || _feePrice == 0)
                        {
                            _maxAmount = 0;
                            OnPropertyChanged(nameof(MaxAmountString));
                            return;
                        }
                    }

                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    _maxAmount = maxAmount;

                    if (maxAmount == 0 && availableAmount > 0)
                        Warning = Resources.CvInsufficientFunds;

                    OnPropertyChanged(nameof(MaxAmountString));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsMaxAmountUpdating = false;
            }
        }

        private async void UpdateSafeMaxAmount()
        {
            if (IsSafeMaxAmountUpdating)
                return;

            IsSafeMaxAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                    if (maxAmount > 0)
                        _safeMaxAmount = maxAmount;

                    OnPropertyChanged(nameof(SafeMaxAmountString));

                    _fee = Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(GasString));

                    _feePrice = await Currency.GetDefaultFeePriceAsync();
                    OnPropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }
                else
                {
                    if (_fee < Currency.GetDefaultFee() || _feePrice == 0)
                    {
                        Warning = Resources.CvLowFees;
                        if (_fee == 0 || _feePrice == 0)
                        {
                            _safeMaxAmount = 0;
                            OnPropertyChanged(nameof(SafeMaxAmountString));
                            return;
                        }
                    }

                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, _fee, _feePrice, true);

                    _safeMaxAmount = maxAmount;

                    if (maxAmount == 0 && availableAmount > 0)
                        Warning = Resources.CvInsufficientFunds;

                    OnPropertyChanged(nameof(SafeMaxAmountString));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsSafeMaxAmountUpdating = false;
            }
        }
        
        protected override async void OnMaxClick()
        {
            if (_maxAmount == 0)
                return;
            
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;
                
                _amount = _maxAmount;
                OnPropertyChanged(nameof(AmountString));
                
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected virtual async void OnSafeMaxClick()
        {
            if (_safeMaxAmount == 0)
                return;
            
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                _amount = _safeMaxAmount;
                OnPropertyChanged(nameof(AmountString));
                
                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
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
            _maxAmount = 123456.12000123m;
            _safeMaxAmount = 123456.12000123m;
            _fee = 0.0001m;
            _feeInBase = 8.43m;
        }

    }
}