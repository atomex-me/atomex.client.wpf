using System;
using Atomix.Blockchain;
using Atomix.Blockchain.Abstract;
using Atomix.Blockchain.Ethereum;

namespace Atomix.Client.Wpf.ViewModels.TransactionViewModels
{
    public class EthereumTransactionViewModel : AddressBasedTransactionViewModel
    {
        public EthereumTransactionViewModel(
            IAddressBasedTransaction tx)
            : base(tx)
        {
        }

        public override decimal GetAmount(IAddressBasedTransaction tx)
        {
            if (!(tx is EthereumTransaction ethTx))
                throw new ArgumentException(nameof(tx));

            var gas = ethTx.GasUsed != 0 ? ethTx.GasUsed : ethTx.GasLimit;

            switch (ethTx.Type)
            {
                case EthereumTransaction.UnknownTransaction:
                    return Ethereum.WeiToEth(ethTx.Amount + ethTx.GasPrice * gas);
                case EthereumTransaction.OutputTransaction:
                    return -Ethereum.WeiToEth(ethTx.Amount + ethTx.GasPrice * gas);
                case EthereumTransaction.InputTransaction:
                    return Ethereum.WeiToEth(ethTx.Amount);
                case EthereumTransaction.SelfTransaction:
                    return -Ethereum.WeiToEth(ethTx.GasPrice * gas);
                default:
                    return 0;
            }
        }

        public override TransactionType GetType(IAddressBasedTransaction tx)
        {
            if (!(tx is EthereumTransaction ethTx))
                throw new ArgumentException(nameof(tx));

            switch (ethTx.Type)
            {
                case EthereumTransaction.UnknownTransaction:
                    return TransactionType.Unknown;
                case EthereumTransaction.OutputTransaction:
                    return TransactionType.Sent;
                case EthereumTransaction.InputTransaction:
                    return TransactionType.Received;
                case EthereumTransaction.SelfTransaction:
                    return TransactionType.Self;
                default:
                    return TransactionType.Unknown;
            }
        }
    }
}