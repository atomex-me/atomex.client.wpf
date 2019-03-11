using System;
using System.Windows;
using System.Windows.Controls;
using Atomix.Client.Wpf.ViewModels;

namespace Atomix.Client.Wpf.Controls
{
    public class SwapStateDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CanceledTemplate { get; set; }
        public DataTemplate InProgressTemplate { get; set; }
        public DataTemplate CompletedTemplate { get; set; }
        public DataTemplate RefundedTemplate { get; set; }

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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}