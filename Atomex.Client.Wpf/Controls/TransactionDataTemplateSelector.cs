using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;

namespace Atomex.Client.Wpf.Controls
{
    public class TransactionDataTemplateSelector : DataTemplateSelector
    {
        public string Currency { get; set; }
        public DataTemplate UnknownTemplate { get; set; }
        public DataTemplate SentTemplate { get; set; }
        public DataTemplate ReceivedTemplate { get; set; }
        public DataTemplate TokenApproveTemplate { get; set; }
        public DataTemplate TokenCallTemplate { get; set; }
        public DataTemplate SwapPaymentTemplate { get; set; }
        public DataTemplate SwapRedeemTemplate { get; set; }
        public DataTemplate SwapRefundTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is TransactionViewModel tx))
                return null;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapPayment))
                return SwapPaymentTemplate;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
                return SwapRefundTemplate;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem))
                return SwapRedeemTemplate;

            if (tx.Type.HasFlag(BlockchainTransactionType.TokenApprove))
                return TokenApproveTemplate;

            if (tx.Type.HasFlag(BlockchainTransactionType.TokenCall))
                return TokenApproveTemplate;

            if (tx.Type.HasFlag(BlockchainTransactionType.SwapCall))
                return TokenApproveTemplate;

            if (tx.Amount <= 0) //tx.Type.HasFlag(BlockchainTransactionType.Output))
                return SentTemplate;

            if (tx.Amount > 0) //tx.Type.HasFlag(BlockchainTransactionType.Input))
                return ReceivedTemplate;

            return UnknownTemplate;
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
                return selector.SelectTemplate(tx, container);
            
            throw new ArgumentOutOfRangeException();
        }
    }
}