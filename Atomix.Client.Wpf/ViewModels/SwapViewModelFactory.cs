using System.Windows.Media;
using Atomix.Common;
using Atomix.Core.Entities;

namespace Atomix.Client.Wpf.ViewModels
{
    public static class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(ClientSwap swap)
        {
            var fromCurrency = CurrencyViewModelCreator.CreateViewModel(
                currency: swap.SoldCurrency,
                subscribeToUpdates: false);

            var toCurrency = CurrencyViewModelCreator.CreateViewModel(
                currency: swap.PurchasedCurrency,
                subscribeToUpdates:false);

            var fromAmount = AmountHelper.QtyToAmount(swap.Side, swap.Qty, swap.Price);
            var toAmount = AmountHelper.QtyToAmount(swap.Side.Opposite(), swap.Qty, swap.Price);

            return new SwapViewModel
            {
                Id = swap.Id.ToString(),
                CompactState = CompactStateBySwap(swap),
                Mode = ModeBySwap(swap),
                Time = swap.TimeStamp,

                FromBrush = new SolidColorBrush(fromCurrency.AmountColor),
                FromAmount = fromAmount,
                FromAmountFormat = fromCurrency.CurrencyFormat,
                FromCurrencyCode = fromCurrency.CurrencyCode,

                ToBrush = new SolidColorBrush(toCurrency.AmountColor),
                ToAmount = toAmount,
                ToAmountFormat = toCurrency.CurrencyFormat,
                ToCurrencyCode = toCurrency.CurrencyCode,

                Price = swap.Price,
                PriceFormat = $"F{swap.Symbol.Quote.Digits}"
            };
        }

        private static SwapMode ModeBySwap(ClientSwap swap)
        {
            return swap.IsInitiator
                ? SwapMode.Initiator
                : SwapMode.CounterParty;
        }

        private static SwapCompactState CompactStateBySwap(ClientSwap swap)
        {
            if (swap.IsComplete)
                return SwapCompactState.Completed;

            if (swap.IsCanceled)
                return SwapCompactState.Canceled;

            return swap.IsRefunded
                ? SwapCompactState.Refunded
                : SwapCompactState.InProgress;
        }
    }
}