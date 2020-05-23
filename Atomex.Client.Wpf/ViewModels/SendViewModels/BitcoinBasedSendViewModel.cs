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

            try
            {
                if (UseDefaultFee)
                {
                    var (maxAmount, maxFeeAmount, _) = await App.Account
                        .EstimateMaxAmountToSendAsync(Currency.Name, To, BlockchainTransactionType.Output, true);

                    var availableAmount = CurrencyViewModel.AvailableAmount;

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

                    _fee = estimatedFeeAmount.Value;
                    OnPropertyChanged(nameof(FeeString));

                    FeeRate = BtcBased.FeeRate;

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

        protected override async void UpdateFee(decimal fee)
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

                var estimatedTxSize = await EstimateTxSizeAsync();

                if (!UseDefaultFee)
                {
                    var minimumFeeSatoshi = BtcBased.GetMinimumFee(estimatedTxSize);
                    var minimumFee = BtcBased.SatoshiToCoin(minimumFeeSatoshi);

                    if (_fee < minimumFee)
                        _fee = minimumFee;

                    var availableAmount = CurrencyViewModel.AvailableAmount;

                    if (_amount + _fee > availableAmount)
                        _amount = Math.Max(availableAmount - _fee, 0);

                    if (_amount == 0)
                        _fee = 0;

                    OnPropertyChanged(nameof(AmountString));
                    OnPropertyChanged(nameof(FeeString));
                    Warning = string.Empty;
                }

                FeeRate = BtcBased.CoinToSatoshi(_fee) / estimatedTxSize;

                OnQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
            }
            finally
            {
                IsFeeUpdating = false;
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