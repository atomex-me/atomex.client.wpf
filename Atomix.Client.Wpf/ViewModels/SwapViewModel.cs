using System;
using System.Windows.Media;

namespace Atomix.Client.Wpf.ViewModels
{
    public enum SwapCompactState
    {
        Canceled,
        InProgress,
        Completed,
        Refunded
    }

    public enum SwapMode
    {
        Initiator,
        CounterParty
    }

    public class SwapViewModel : BaseViewModel
    {
        public string Id { get; set; }

        public SwapCompactState CompactState { get; set; }
        public SwapMode Mode { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();

        public Brush FromBrush { get; set; }
        public decimal FromAmount { get; set; }
        public string FromAmountFormat { get; set; }
        public string FromCurrencyCode { get; set; }

        public Brush ToBrush { get; set; }
        public decimal ToAmount { get; set; }
        public string ToAmountFormat { get; set; }
        public string ToCurrencyCode { get; set; }

        public decimal Price { get; set; }
        public string PriceFormat { get; set; }

        public string State
        {
            get
            {
                switch (CompactState)
                {
                    case SwapCompactState.Canceled:
                        return "Canceled";
                    case SwapCompactState.InProgress:
                        return "In Progress";
                    case SwapCompactState.Completed:
                        return "Completed";
                    case SwapCompactState.Refunded:
                        return "Refunded";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}