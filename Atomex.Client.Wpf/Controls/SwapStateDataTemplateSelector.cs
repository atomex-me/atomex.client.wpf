using System;
using System.Windows;
using System.Windows.Controls;
using Atomex.Client.Wpf.ViewModels;

namespace Atomex.Client.Wpf.Controls
{
    public class SwapStateDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CanceledTemplate { get; set; }
        public DataTemplate InProgressTemplate { get; set; }
        public DataTemplate CompletedTemplate { get; set; }
        public DataTemplate RefundedTemplate { get; set; }
        public DataTemplate UnsettledTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is SwapViewModel swap))
                return null;

            switch (swap.CompactState)
            {
                case SwapCompactState.Canceled:
                    return CanceledTemplate;
                case SwapCompactState.InProgress:
                    return InProgressTemplate;
                case SwapCompactState.Completed:
                    return CompletedTemplate;
                case SwapCompactState.Refunded:
                    return RefundedTemplate;
                case SwapCompactState.Unsettled:
                    return UnsettledTemplate;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}