using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Core;
using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        protected decimal _feeRate;
        public decimal FeeRate
        {
            get => _feeRate;
            set { _feeRate = value; OnPropertyChanged(nameof(FeeRate)); }
        }

        private BitcoinBasedCurrency BtcBased => Currency as BitcoinBasedCurrency;

        public BitcoinBasedSendViewModel()
            : base()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                BitcoinBasedDesignerMode();
#endif
        }

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer, currency)
        {
        }

        protected override async void UpdateAmount(decimal amount)
        {
            IsAmountUpdating = true;

            var previousAmount = _amount;
            _amount = amount;

            Warning = string.Empty;

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (_amount > maxAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }

                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    var estimatedFeeAmount = _amount != 0
                        ? await App.Account.EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output)
                        : 0;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = estimatedFeeAmount ?? Currency.GetDefaultFee();
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = BtcBased.FeeRate;
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

                    if (_amount + feeAmount > availableAmount)
                    {
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
                var availableAmount = CurrencyViewModel.AvailableAmount;

                if (_amount == 0)
                {
                    if (Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice()) > availableAmount)
                        Warning = Resources.CvInsufficientFunds;
                    return;
                }
                
                var estimatedTxSize = await EstimateTxSizeAsync();

                if (!UseDefaultFee)
                {
                    var minimumFeeSatoshi = BtcBased.GetMinimumFee(estimatedTxSize);
                    var minimumFee = BtcBased.SatoshiToCoin(minimumFeeSatoshi);

                    if (_amount + _fee > availableAmount)
                    {
                        Warning = Resources.CvInsufficientFunds;
                        IsAmountUpdating = false;
                        return;
                    }
                    if (_fee < minimumFee)
                        Warning = Resources.CvLowFees;

                    OnPropertyChanged(nameof(AmountString));
                    OnPropertyChanged(nameof(FeeString));
                }

                FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize;

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
                if (CurrencyViewModel.AvailableAmount == 0)
                    return;

                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    if (maxAmount > 0)
                        _amount = maxAmount;

                    OnPropertyChanged(nameof(AmountString));

                    _fee = Currency.GetFeeFromFeeAmount(maxFeeAmount, Currency.GetDefaultFeePrice());
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = BtcBased.FeeRate;
                }
                else
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output);

                    var availableAmount = Currency is BitcoinBasedCurrency
                        ? CurrencyViewModel.AvailableAmount
                        : maxAmount + maxFeeAmount;

                    var feeAmount = Currency.GetFeeAmount(_fee, Currency.GetDefaultFeePrice());

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
                }

                var estimatedTxSize = await EstimateTxSizeAsync();
                FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize;

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

        private async Task<int> EstimateTxSizeAsync()
        {
            var estimatedFee = await App.Account
                .EstimateFeeAsync(Currency.Name, To, _amount, BlockchainTransactionType.Output);

            if (estimatedFee == null)
                return 0;

            return (int)(BtcBased.CoinToSatoshi(estimatedFee.Value) / BtcBased.FeeRate);
        }

        private void BitcoinBasedDesignerMode()
        {
            _feeRate = 200;
        }
    }
}