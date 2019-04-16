using System;
using System.Windows;
using System.Windows.Controls;
using Atomix.Blockchain;
using Atomix.Client.Wpf.ViewModels.TransactionViewModels;

namespace Atomix.Client.Wpf.Controls
{
    public class TransactionTypeDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SentTemplate { get; set; }
        public DataTemplate ReceivedTemplate { get; set; }
        public DataTemplate InternalTemplate { get; set; }
        public DataTemplate ExchangedTemplate { get; set; }
        public DataTemplate RefundedTemplate { get; set; }
        public DataTemplate UnknownTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is TransactionViewModel transaction))
                return null;

            switch (transaction.Type)
            {
                case TransactionType.Sent:
                    return SentTemplate;
                case TransactionType.Received:
                    return ReceivedTemplate;
                case TransactionType.Self:
                    return InternalTemplate;
                //case TransactionType.Exchanged:
                //    return ExchangedTemplate;
                //case TransactionType.Refunded:
                //    return RefundedTemplate;
                case TransactionType.Unknown:
                    return UnknownTemplate;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}