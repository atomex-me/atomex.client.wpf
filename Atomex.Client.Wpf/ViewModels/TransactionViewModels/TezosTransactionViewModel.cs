using System;
using Atomex.Blockchain;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Controls;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TezosTransactionViewModel : AddressBasedTransactionViewModel
    {
        public TezosTransactionViewModel(IAddressBasedTransaction tx)
            : base(tx)
        {
        }

        public override decimal GetAmount(IAddressBasedTransaction tx)
        {
            if (!(tx is TezosTransaction xtzTx))
                throw new ArgumentException(nameof(tx));

            switch (xtzTx.Type)
            {
                //case TezosTransaction.UnknownTransaction:
                case TezosTransaction.OutputTransaction:
                    return -Tezos.MtzToTz(xtzTx.Amount + xtzTx.Fee);
                case TezosTransaction.InputTransaction:
                    return Tezos.MtzToTz(xtzTx.Amount);
                case TezosTransaction.SelfTransaction:
                    return -Tezos.MtzToTz(xtzTx.Fee);
                default:
                    return 0;
            }
        }

        public override TransactionType GetType(IAddressBasedTransaction tx)
        {
            if (!(tx is TezosTransaction xtzTx))
                throw new ArgumentException(nameof(tx));

            switch (xtzTx.Type)
            {
                //case TezosTransaction.UnknownTransaction:
                //    return TransactionType.Unknown;
                case TezosTransaction.OutputTransaction:
                    return TransactionType.Sent;
                case TezosTransaction.InputTransaction:
                    return TransactionType.Received;
                case TezosTransaction.SelfTransaction:
                    return TransactionType.Self;
                default:
                    return TransactionType.Unknown;
            }
        }
    }
}