using Atomex.Blockchain.Abstract;
using System.Collections.Generic;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class BitcoinBasedTransactionViewModel : InOutTransactionViewModel
    {
        public BitcoinBasedTransactionViewModel(
            IInOutTransaction tx,
            IDictionary<string, ITxOutput> indexedOutputs)
            : base(tx, indexedOutputs)
        {
        }
    }
}