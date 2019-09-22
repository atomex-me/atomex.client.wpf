using Atomex.Blockchain.BitcoinBased;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(IBitcoinBasedTransaction tx)
            : base(tx, tx.Amount / (decimal)tx.Currency.DigitsMultiplier)
        {
            Fee = tx.Fees != null
                ? tx.Fees.Value / (decimal)tx.Currency.DigitsMultiplier
                : 0; // todo: N/A
        }
    }
}