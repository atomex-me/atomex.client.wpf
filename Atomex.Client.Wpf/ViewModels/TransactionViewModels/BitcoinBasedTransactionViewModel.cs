using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : TransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(IBitcoinBasedTransaction tx)
            : base(tx, tx.Amount / (decimal)tx.Currency.DigitsMultiplier, GetFee(tx))
        {
            Fee = tx.Fees != null
                ? tx.Fees.Value / (decimal)tx.Currency.DigitsMultiplier
                : 0; // todo: N/A        
        }

        private static decimal GetFee(IBitcoinBasedTransaction tx)
        {
            return tx.Fees != null
                ? tx.Type.HasFlag(BlockchainTransactionType.Output) 
                    ? tx.Fees.Value / (decimal)tx.Currency.DigitsMultiplier
                    : 0
                : 0;
        }
    }
}