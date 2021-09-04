using System;
using System.Windows.Media;

using Atomex.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Core;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(Swap swap, ICurrencies currencies)
        {
            try
            {
                var soldCurrency = currencies.GetByName(swap.SoldCurrency);
                var purchasedCurrency = currencies.GetByName(swap.PurchasedCurrency);

                var fromCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(
                    currencyConfig: soldCurrency,
                    subscribeToUpdates: false);

                var toCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(
                    currencyConfig: purchasedCurrency,
                    subscribeToUpdates:false);

                var fromAmount = AmountHelper.QtyToAmount(swap.Side, swap.Qty, swap.Price, soldCurrency.DigitsMultiplier);
                var toAmount = AmountHelper.QtyToAmount(swap.Side.Opposite(), swap.Qty, swap.Price, purchasedCurrency.DigitsMultiplier);

                var quoteCurrency = swap.Symbol.QuoteCurrency() == swap.SoldCurrency
                    ? soldCurrency
                    : purchasedCurrency;

                return new SwapViewModel
                {
                    Id               = swap.Id.ToString(),
                    CompactState     = CompactStateBySwap(swap),
                    Mode             = ModeBySwap(swap),
                    Time             = swap.TimeStamp,

                    FromBrush        = new SolidColorBrush(fromCurrencyViewModel.AmountColor),
                    FromAmount       = fromAmount,
                    FromAmountFormat = fromCurrencyViewModel.CurrencyFormat,
                    FromCurrencyCode = fromCurrencyViewModel.CurrencyCode,

                    ToBrush          = new SolidColorBrush(toCurrencyViewModel.AmountColor),
                    ToAmount         = toAmount,
                    ToAmountFormat   = toCurrencyViewModel.CurrencyFormat,
                    ToCurrencyCode   = toCurrencyViewModel.CurrencyCode,

                    Price            = swap.Price,
                    PriceFormat      = $"F{quoteCurrency.Digits}"
                };
            }
            catch (Exception e)
            {
                Log.Error(e, $"Error while create SwapViewModel for {swap.Symbol} swap with id {swap.Id}");

                return null;
            }
        }

        private static SwapMode ModeBySwap(Swap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }

        private static SwapCompactState CompactStateBySwap(Swap swap)
        {
            if (swap.IsComplete)
                return SwapCompactState.Completed;

            if (swap.IsCanceled)
                return SwapCompactState.Canceled;

            if (swap.IsUnsettled)
                return SwapCompactState.Unsettled;

            if (swap.IsRefunded)
                return SwapCompactState.Refunded;

            return SwapCompactState.InProgress;
    }
}
}