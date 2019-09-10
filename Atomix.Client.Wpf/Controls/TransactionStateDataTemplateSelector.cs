using System;
using System.Windows;
using System.Windows.Controls;
using Atomix.Client.Wpf.ViewModels.TransactionViewModels;

namespace Atomix.Client.Wpf.Controls
{
    public enum TransactionState
    {
        Unconfirmed,
        Confirmed
    }

    public class TransactionStateDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ConfirmedTemplate { get; set; }
        public DataTemplate UnconfirmedTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is TransactionViewModel transaction))
                return null;

            switch (transaction.State)
            {
                case TransactionState.Confirmed:
                    return ConfirmedTemplate;
                case TransactionState.Unconfirmed:
                    return UnconfirmedTemplate;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}