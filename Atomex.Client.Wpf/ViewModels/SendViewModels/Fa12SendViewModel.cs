using System;
using System.Globalization;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;
using Atomex.MarketData.Abstract;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class Fa12SendViewModel : SendViewModel
    {
        public Fa12SendViewModel()
            : base()
        {
        }

        public Fa12SendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            CurrencyConfig currency)
            : base(app, dialogViewer, currency)
        {
        }

        public override bool UseDefaultFee
        {
            get => _useDefaultFee;
            set
            {
                Warning = string.Empty;

                _useDefaultFee = value;
                OnPropertyChanged(nameof(UseDefaultFee));

                Amount = _amount; // recalculate amount
            }
        }

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            var availableAmount = CurrencyViewModel.AvailableAmount;
            _amount = amount;

            Warning = string.Empty;

            try
            {
                var account = App.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, 0, 0, false);

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = Resources.CvInsufficientFunds;

                        IsAmountUpdating = false;
                        return;
                    }

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    _fee = Currency.GetFeeFromFeeAmount(estimatedFeeAmount ?? Currency.GetDefaultFee(), defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var (maxAmount, _, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, 0, 0, false);

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = Resources.CvInsufficientFunds;

                        IsAmountUpdating = false;
                        return;
                    }

                    OnPropertyChanged(nameof(AmountString));

                    Fee = _fee;
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
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
                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    if (Currency.GetFeeAmount(_fee, defaultFeePrice) > CurrencyViewModel.AvailableAmount)
                        Warning = Resources.CvInsufficientFunds;

                    return;
                }

                if (!UseDefaultFee)
                {
                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    var account = App.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    var (maxAmount, maxAvailableFee, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, decimal.MaxValue, 0, false);

                    var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    var estimatedFeeAmount = _amount != 0
                        ? await account.EstimateFeeAsync(To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    if (_amount > maxAmount)
                    {
                        if (_amount <= availableAmount)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);
                        else
                            Warning = Resources.CvInsufficientFunds;

                        return;
                    }
                    else if (estimatedFeeAmount == null || feeAmount < estimatedFeeAmount.Value)
                    {
                        Warning = Resources.CvLowFees;
                    }

                    if (feeAmount > maxAvailableFee)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
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

                var defaultFeePrice = await Currency.GetDefaultFeePriceAsync();

                var account = App.Account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, 0, 0, true);

                    if (maxAmount > 0)
                        _amount = maxAmount;
                    else
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, defaultFeePrice);
                    OnPropertyChanged(nameof(FeeString));
                }
                else
                {
                    var (maxAmount, maxFee, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, 0, 0, false);

                    var feeAmount = Currency.GetFeeAmount(_fee, defaultFeePrice);

                    if (_fee < maxFee)
                    {
                        Warning = Resources.CvLowFees;
                        if (_fee == 0)
                        {
                            _amount = 0;
                            OnPropertyChanged(nameof(AmountString));
                            return;
                        }
                    }

                    _amount = maxAmount;

                    var (_, maxAvailableFee, _) = await account
                        .EstimateMaxAmountToSendAsync(To, BlockchainTransactionType.Output, decimal.MaxValue, 0, false);

                    if (maxAmount < availableAmount || feeAmount > maxAvailableFee)
                        Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, Currency.FeeCurrencyName);

                    OnPropertyChanged(nameof(AmountString));

                    OnPropertyChanged(nameof(FeeString));
                }

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        protected override void OnQuotesUpdatedEventHandler(object sender, EventArgs args)
        {
            if (!(sender is ICurrencyQuotesProvider quotesProvider))
                return;

            var quote = quotesProvider.GetQuote(CurrencyCode, BaseCurrencyCode);
            var xtzQuote = quotesProvider.GetQuote("XTZ", BaseCurrencyCode);

            AmountInBase = Amount * (quote?.Bid ?? 0m);
            FeeInBase = Fee * (xtzQuote?.Bid ?? 0m);
        }
    }
}