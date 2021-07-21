using System;
using System.Windows;
using System.Windows.Controls;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;

namespace Atomex.Client.Wpf.Controls
{
    public class TransactionStateDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UnknownTemplate { get; set; }
        public DataTemplate PendingTemplate { get; set; }
        public DataTemplate ConfirmedTemplate { get; set; }
        public DataTemplate UnconfirmedTemplate { get; set; }
        public DataTemplate FailedTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is ITransactionViewModel transaction))
                return null;

            switch (transaction.State)
            {
                case BlockchainTransactionState.Unknown:
                    return UnknownTemplate;
                case BlockchainTransactionState.Pending:
                    return PendingTemplate;
                case BlockchainTransactionState.Confirmed:
                    return ConfirmedTemplate;
                case BlockchainTransactionState.Unconfirmed:
                    return UnconfirmedTemplate;
                case BlockchainTransactionState.Failed:
                    return FailedTemplate;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}