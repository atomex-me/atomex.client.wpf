using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Abstract;
using Atomex.Blockchain.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.MarketData;
using Atomex.MarketData.Abstract;
using Atomex.Subsystems;
using Atomex.Subsystems.Abstract;
using Atomex.Swaps;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class TokensConversionViewModel : ConversionViewModel
    {
        public TokensConversionViewModel()
            : base()
        {
        }

        public TokensConversionViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer)
        {
            _currency = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == currency.Name)
                .Currency;
        }

        protected Currency _currency;

        protected override async void UpdateAmount(decimal value)
        {
            Warning = string.Empty;

            try
            {
                IsAmountUpdating = true;

                var previousAmount = _amount;
                _amount = value;

                var (maxAmount, maxFee, reserve) = await App.Account
                    .EstimateMaxAmountToSendAsync(FromCurrency.Name, null, BlockchainTransactionType.SwapPayment);

                var swaps = await App.Account
                    .GetSwapsAsync();

                var usedAmount = swaps.Sum(s => (s.IsActive && s.SoldCurrency == FromCurrency.Name && !s.StateFlags.HasFlag(SwapStateFlags.IsPaymentConfirmed))
                    ? s.Symbol.IsBaseCurrency(FromCurrency.Name)
                        ? s.Qty
                        : s.Qty * s.Price
                    : 0);

                usedAmount = AmountHelper.RoundDown(usedAmount, FromCurrency.DigitsMultiplier);

                maxAmount = Math.Max(maxAmount - usedAmount, 0);

                var includeFeeToAmount = FromCurrency.FeeCurrencyName == FromCurrency.Name;

                var availableAmount = FromCurrency is BitcoinBasedCurrency
                    ? FromCurrencyViewModel.AvailableAmount
                    : maxAmount + (includeFeeToAmount ? maxFee : 0);

                var estimatedPaymentFee = _amount != 0
                    ? (_amount < availableAmount
                        ? await App.Account
                            .EstimateFeeAsync(FromCurrency.Name, null, _amount, BlockchainTransactionType.SwapPayment)
                        : null)
                    : 0;

                if (estimatedPaymentFee == null)
                {
                    if (maxAmount > 0)
                    {
                        _amount = maxAmount;
                        estimatedPaymentFee = maxFee;
                    }
                    else
                    {
                        _amount = 0; // previousAmount;
                        OnPropertyChanged(nameof(Amount));

                        if(FromCurrencyViewModel.AvailableAmount > 0)
                            Warning = string.Format(CultureInfo.InvariantCulture, Resources.CvInsufficientChainFunds, FromCurrency.FeeCurrencyName);

                        IsAmountUpdating = false;
                        return;
                    }
                }

                EstimatedPaymentFee = estimatedPaymentFee.Value;

                if (_amount + (includeFeeToAmount ? _estimatedPaymentFee : 0) > availableAmount)
                    _amount = Math.Max(availableAmount - (includeFeeToAmount ? _estimatedPaymentFee : 0), 0);

                OnPropertyChanged(nameof(CurrencyFormat));
                OnPropertyChanged(nameof(TargetCurrencyFormat));
                OnPropertyChanged(nameof(Amount));

                UpdateRedeemAndRewardFeesAsync();

                //var walletAddress = await App.Account
                //    .GetRedeemAddressAsync(ToCurrency.FeeCurrencyName);
                //EstimatedRedeemFee = ToCurrency.GetRedeemFee(walletAddress);

                //RewardForRedeem = walletAddress.AvailableBalance() < EstimatedRedeemFee && !(ToCurrency is BitcoinBasedCurrency)
                //    ? ToCurrency.GetRewardForRedeem()
                //    : 0;

                //OnPropertyChanged(nameof(EstimatedPaymentFee));
                //OnPropertyChanged(nameof(EstimatedRedeemFee));
                //OnPropertyChanged(nameof(RewardForRedeem));

#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    OnQuotesUpdatedEventHandler(App.Terminal, null);
                    OnBaseQuotesUpdatedEventHandler(App.QuotesProvider, EventArgs.Empty);
#if DEBUG
                }
#endif
            }
            finally
            {
                IsAmountUpdating = false;
            }
        }

     }
}