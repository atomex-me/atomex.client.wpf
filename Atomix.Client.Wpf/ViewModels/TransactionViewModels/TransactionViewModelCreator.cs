using System.Collections.Generic;
using System.Linq;
using Atomix.Blockchain.Abstract;

namespace Atomix.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TransactionViewModelCreator
    {
        private Dictionary<string, ITxOutput> IndexedOutputs { get; }

        public TransactionViewModelCreator(
            IEnumerable<IBlockchainTransaction> transactions,
            IEnumerable<ITxOutput> outputs)
        {
            IndexedOutputs = outputs.ToDictionary(o => $"{o.TxId}:{o.Index}");
        }

        public TransactionViewModel CreateViewModel(IBlockchainTransaction tx)
        {
            switch (tx.Currency)
            {
                case BitcoinBasedCurrency _:
                    return new BitcoinBasedTransactionViewModel((IInOutTransaction)tx, IndexedOutputs);
                case Ethereum _:
                    return new EthereumTransactionViewModel((IAddressBasedTransaction)tx);
                case Tezos _:
                    return new TezosTransactionViewModel((IAddressBasedTransaction)tx);
            }

            return null;
        }      
    }
}