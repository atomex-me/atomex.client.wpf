using System;
using System.Globalization;
using System.Linq;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class EthereumSendViewModel : SendViewModel
    {
        public override CurrencyConfig Currency
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

        protected decimal _feePrice;
        public virtual decimal FeePrice
        {
            get => _feePrice;
            set { UpdateFeePrice(value); }
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
                    Amount = _amount; // recalculate amount and fee using default fee
            }
        }

        protected string _feePriceCode;
        public string FeePriceCode
        {
            get => _feePriceCode;
            set { _feePriceCode = value; OnPropertyChanged(nameof(FeePriceCode)); }
        }

        public EthereumSendViewModel()
            : base()
        {
        }

        public EthereumSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            CurrencyConfig currency)
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
                var account = _app.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, 0, 0, false);

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
                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, _fee, _feePrice, false);

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

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private async void UpdateFeePrice(decimal value)
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
                    var account = _app.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    OnPropertyChanged(nameof(FeePriceString));

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
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
                    var account = _app.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        return;
                    }

                    UpdateTotalFeeString();
                    OnPropertyChanged(nameof(TotalFeeString));

                    OnPropertyChanged(nameof(GasString));
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
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
                var account = _app.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                var feeAmount = totalFeeAmount > 0
                    ? totalFeeAmount
                    : Currency.GetFeeAmount(_fee, _feePrice) > 0
                        ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output, _fee, _feePrice)
                        : 0;

                if (feeAmount != null)
                    _totalFee = feeAmount.Value;
            }
            finally
            {
                IsTotalFeeUpdating = false;
            }
        }

        protected override async void OnMaxClick()
        {
            if (IsAmountUpdating)
                return;

            IsAmountUpdating = true;

            Warning = string.Empty;

            try
            {
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (availableAmount == 0)
                    return;

                var account = _app.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, 0, 0, false);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

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
                            _amount = 0;
                            OnPropertyChanged(nameof(AmountString));
                            return;
                        }
                    }

                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, _fee, _feePrice, false);

                    _amount = maxAmount;

                    if (maxAmount == 0 && availableAmount > 0)
                        Warning = Resources.CvInsufficientFunds;

                    OnPropertyChanged(nameof(AmountString));

                    UpdateTotalFeeString(maxFeeAmount);
                    OnPropertyChanged(nameof(TotalFeeString));
                }

                OnQuotesUpdatedEventHandler(_app.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
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

            var confirmationViewModel = new SendConfirmationViewModel(_dialogViewer, Dialogs.Send)
            {
                Currency           = Currency,
                To                 = To,
                Amount             = Amount,
                AmountInBase       = AmountInBase,
                BaseCurrencyCode   = BaseCurrencyCode,
                BaseCurrencyFormat = BaseCurrencyFormat,
                Fee                = Fee,
                UseDeafultFee      = UseDefaultFee,
                FeeInBase          = FeeInBase,
                FeePrice           = FeePrice,
                CurrencyCode       = CurrencyCode,
                CurrencyFormat     = CurrencyFormat,

                FeeCurrencyCode    = FeeCurrencyCode,
                FeeCurrencyFormat  = FeeCurrencyFormat
            };

            _dialogViewer.PushPage(Dialogs.Send, Pages.SendConfirmation, confirmationViewModel);
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Currency.GetFeeAmount(Fee, FeePrice) * (quote?.Bid ?? 0m);
        }


    }
}