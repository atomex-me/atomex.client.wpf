using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TransactionViewModelCreator
    {
        public TransactionViewModel CreateViewModel(IBlockchainTransaction tx)
        {
            switch (tx.Currency)
            {
                case BitcoinBasedCurrency _:
                    return new BitcoinBasedTransactionViewModel((IBitcoinBasedTransaction)tx);
                case Ethereum _:
                    return new EthereumTransactionViewModel((EthereumTransaction)tx);
                case Tezos _:
                    return new TezosTransactionViewModel((TezosTransaction)tx);
            }

            return null;
        }      
    }
}