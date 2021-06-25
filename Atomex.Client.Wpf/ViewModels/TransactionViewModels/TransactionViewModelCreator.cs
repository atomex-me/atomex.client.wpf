using System;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.BitcoinBased;
using Atomex.Blockchain.Ethereum;
using Atomex.Blockchain.Tezos;
using Atomex.Core;
using Atomex.EthereumTokens;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public static class TransactionViewModelCreator
    {
        public static TransactionViewModel CreateViewModel(
            IBlockchainTransaction tx,
            CurrencyConfig currencyConfig)
        {
            return tx.Currency switch
            {
                "BTC"   => (TransactionViewModel)new BitcoinBasedTransactionViewModel(tx as IBitcoinBasedTransaction, currencyConfig as BitcoinBasedConfig),
                "LTC"   => new BitcoinBasedTransactionViewModel(tx as IBitcoinBasedTransaction, currencyConfig as BitcoinBasedConfig),
                "USDT"  => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config),
                "TBTC"  => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config),
                "WBTC"  => new EthereumERC20TransactionViewModel(tx as EthereumTransaction, currencyConfig as Erc20Config),
                "ETH"   => new EthereumTransactionViewModel(tx as EthereumTransaction, currencyConfig as EthereumConfig),
                "FA2"   => new TezosTokenTransferViewModel(tx as TokenTransfer, currencyConfig),
                "FA12"  => new TezosTokenTransferViewModel(tx as TokenTransfer, currencyConfig),
                "TZBTC" => new TezosTokenTransferViewModel(tx as TokenTransfer, currencyConfig),
                "KUSD"  => new TezosTokenTransferViewModel(tx as TokenTransfer, currencyConfig),
                "XTZ"   => new TezosTransactionViewModel(tx as TezosTransaction, currencyConfig as TezosConfig),
                _ => throw new NotSupportedException("Not supported transaction type."),
            };
        }      
    }
}