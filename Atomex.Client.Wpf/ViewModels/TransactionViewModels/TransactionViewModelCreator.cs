using System;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;
using Atomex.EthereumTokens;
using Atomex.TezosTokens;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(IBlockchainTransaction tx)
        {
            return tx.Currency switch
            {
                BitcoinBasedCurrency _ => (TransactionViewModel)new BitcoinBasedTransactionViewModel((IBitcoinBasedTransaction)tx),
                Tether _ => (TransactionViewModel)new EthereumERC20TransactionViewModel((EthereumTransaction)tx),
                Ethereum _ => (TransactionViewModel)new EthereumTransactionViewModel((EthereumTransaction)tx),
                TZBTC _ => (TransactionViewModel)new TezosTransactionViewModel((TezosTransaction)tx),
                FA12 _ => (TransactionViewModel)new TezosTransactionViewModel((TezosTransaction)tx),
                Tezos _ => (TransactionViewModel)new TezosTransactionViewModel((TezosTransaction)tx),
                _ => throw new NotSupportedException("Not supported transaction type."),
            };
        }      
    }
}