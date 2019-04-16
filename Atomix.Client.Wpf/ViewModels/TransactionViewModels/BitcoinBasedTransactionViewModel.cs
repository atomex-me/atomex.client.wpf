using Atomix.Blockchain.Abstract;
using System.Collections.Generic;

namespace Atomix.Client.Wpf.ViewModels.TransactionViewModels
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