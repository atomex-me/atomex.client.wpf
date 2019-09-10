using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Atomix.Client.Wpf.ViewModels.TransactionViewModels;

namespace Atomix.Client.Wpf.Controls
{
    public enum TransactionType
    {
        Unknown,
        Sent,
        Received,
        Self,
        SwapPayment,
        SwapRedeem,
        SwapRefund
    }

    public class TransactionDataTemplateSelector : DataTemplateSelector
    {
        public string Currency { get; set; }
        public DataTemplate UnknownTemplate { get; set; }
        public DataTemplate SentTemplate { get; set; }
        public DataTemplate ReceivedTemplate { get; set; }
        public DataTemplate SelfTemplate { get; set; }
        public DataTemplate SwapPaymentTemplate { get; set; }
        public DataTemplate SwapRedeemTemplate { get; set; }
        public DataTemplate SwapRefundTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is TransactionViewModel transaction))
                return null;

            switch (transaction.Type)
            {
                case TransactionType.Unknown:
                    return UnknownTemplate;
                case TransactionType.Sent:
                    return SentTemplate;
                case TransactionType.Received:
                    return ReceivedTemplate;
                case TransactionType.Self:
                    return SelfTemplate;
                case TransactionType.SwapPayment:
                    return SwapPaymentTemplate;
                case TransactionType.SwapRedeem:
                    return SwapRedeemTemplate;
                case TransactionType.SwapRefund:
                    return SwapRefundTemplate;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class TransactionDataTemplateSelectorList : List<TransactionDataTemplateSelector> { }

    public class TransactionDataTemplateByCurrencySelector : DataTemplateSelector
    {
        public TransactionDataTemplateSelectorList Selectors { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is TransactionViewModel tx))
                return null;

            var selector = Selectors.FirstOrDefault(s => s.Currency == tx.Currency.Name);

            if (selector != null)
            {
                return selector.SelectTemplate(tx, container);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}