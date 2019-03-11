using System;
using System.Windows.Media;
using Atomix.Common;
using Atomix.Core.Entities;
using Atomix.Swaps;
using Atomix.Swaps.Abstract;

namespace Atomix.Client.Wpf.ViewModels
{
    public class SwapViewModelFactory
    {
        public static SwapViewModel CreateSwapViewModel(Order order)
        {
            return new SwapViewModel
            {
                Id = "N/A",
                CompactState = SwapCompactState.Canceled,
                Mode = SwapMode.Initiator,
                Time = order.TimeStamp,
            };
        }

        public static SwapViewModel CreateSwapViewModel(ISwap s)
        {
            if (s is Swap swap)
            {
                var order = swap.Order;

                var fromCurrency = CurrencyViewModelCreator.CreateViewModel(
                    currency: order.SoldCurrency(),
                    subscribeToUpdates: false);

                var toCurrency = CurrencyViewModelCreator.CreateViewModel(
                    currency: order.PurchasedCurrency(),
                    subscribeToUpdates:false);

                var fromAmount = AmountHelper.QtyToAmount(order.Side, order.LastQty, order.LastPrice);
                var toAmount = AmountHelper.QtyToAmount(order.Side.Opposite(), order.LastQty, order.LastPrice);

                return new SwapViewModel
                {
                    Id = swap.Id.ToString(),
                    CompactState = CompactStateBySwap(swap),
                    Mode = ModeBySwap(swap),
                    Time = order.TimeStamp,

                    FromBrush = new SolidColorBrush(fromCurrency.AmountColor),
                    FromAmount = fromAmount,
                    FromAmountFormat = fromCurrency.CurrencyFormat,
                    FromCurrencyCode = fromCurrency.CurrencyCode,

                    ToBrush = new SolidColorBrush(toCurrency.AmountColor),
                    ToAmount = toAmount,
                    ToAmountFormat = toCurrency.CurrencyFormat,
                    ToCurrencyCode = toCurrency.CurrencyCode,

                    Price = order.LastPrice,
                    PriceFormat = $"F{order.Symbol.PriceDigits}"
                };
            }

            throw new NotSupportedException("Swap not supported");
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

            return swap.IsRefunded
                ? SwapCompactState.Refunded
                : SwapCompactState.InProgress;
        }
    }
}