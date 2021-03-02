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

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel()
            : base()
        {
            MaxAmount = 0;
        }

        public TezosSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer, currency)
        {
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
            }
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
            get => SafeMaxAmount.ToString(CurrencyFormat, CultureInfo.InvariantCulture);
            set
            {
                if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var safeAmount))
                    return;

                SafeMaxAmount = safeAmount.TruncateByFormat(CurrencyFormat);
            }
        }
        
        protected ICommand _safeMaxCommand;
        public ICommand SafeMaxCommand => _safeMaxCommand ?? (_safeMaxCommand = new Command(OnSafeMaxClick));
        
        protected virtual async void UpdateMaxAmount()
        {
            if (IsMaxAmountUpdating)
                return;

            IsMaxAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                    if (maxAmount > 0)
                        _maxAmount = maxAmount;

                    OnPropertyChanged(nameof(MaxAmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (availableAmount - feeAmount > 0)
                    {
                        _maxAmount = availableAmount - feeAmount;

                        var estimatedFeeAmount = _maxAmount != 0
                            ? await App.Account.EstimateFeeAsync(Currency.Name, To, _maxAmount, BlockchainTransactionType.Output)
                            : 0;

                        if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                        {
                            Warning = Resources.CvLowFees;
                            if (_fee == 0)
                            {
                                _maxAmount = 0;
                                OnPropertyChanged(nameof(MaxAmountString));
                                return;
                            }
                        }
                    }
                    else
                    {
                        _maxAmount = 0;

                        Warning = Resources.CvInsufficientFunds;
                    }

                    OnPropertyChanged(nameof(MaxAmountString));

                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsMaxAmountUpdating = false;
            }

        }
        
                protected virtual async void UpdateSafeMaxAmount()
        {
            if (IsSafeMaxAmountUpdating)
                return;

            IsSafeMaxAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                    if (maxAmount > 0)
                        _safeMaxAmount = maxAmount;

                    OnPropertyChanged(nameof(SafeMaxAmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (availableAmount - feeAmount > 0)
                    {
                        _safeMaxAmount = availableAmount - feeAmount;

                        var estimatedFeeAmount = _safeMaxAmount != 0
                            ? await App.Account.EstimateFeeAsync(Currency.Name, To, _safeMaxAmount, BlockchainTransactionType.Output)
                            : 0;

                        if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                        {
                            Warning = Resources.CvLowFees;
                            if (_fee == 0)
                            {
                                _maxAmount = 0;
                                OnPropertyChanged(nameof(SafeMaxAmountString));
                                return;
                            }
                        }
                    }
                    else
                    {
                        _safeMaxAmount = 0;

                        Warning = Resources.CvInsufficientFunds;
                    }

                    OnPropertyChanged(nameof(SafeMaxAmountString));

                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsSafeMaxAmountUpdating = false;
            }

        }
                
               protected virtual async void OnMaxClick()
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

                if (UseDefaultFee)
                {
                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, false);
                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }

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

                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();


                var (maxAmount, maxFeeAmount, _) = await App.Account
                    .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, 0, 0, true);

                var availableAmount = Currency is BitcoinBasedCurrency
                    ? CurrencyViewModel.AvailableAmount
                    : maxAmount + maxFeeAmount;

                var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                if (availableAmount - feeAmount > 0)
                {
                    _amount = availableAmount - feeAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
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