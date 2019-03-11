using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atomix.Blockchain;
using Atomix.Blockchain.Abstract;
using Atomix.Blockchain.Ethereum;
using Atomix.Core.Entities;
using Atomix.Client.Wpf.ViewModels.Abstract;

namespace Atomix.Client.Wpf.ViewModels
{
    public class TransactionViewModelFactory
    {
        private Currency Currency { get; }
        private CurrencyViewModel CurrencyViewModel { get; }
        //private Dictionary<string, IBlockchainTransaction> IndexedTransactions { get; }
        private Dictionary<string, ITxOutput> IndexedOutputs { get; }

        public TransactionViewModelFactory(Currency currency,
            IEnumerable<IBlockchainTransaction> transactions,
            IEnumerable<ITxOutput> outputs)
        {
            Currency = currency;
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency, false);

            IndexedOutputs = outputs.ToDictionary(o => $"{o.TxId}:{o.Index}");
            //IndexedTransactions = transactions.ToDictionary(transaction => transaction.Id);
        }

        public TransactionViewModel CreateViewModel(IBlockchainTransaction transaction)
        {
            switch (transaction)
            {
                case IInOutTransaction inOutTransaction:
                    return CreateViewModel(inOutTransaction);
                case IAddressBasedTransaction addressBasedTransaction:
                    return CreateViewModel(addressBasedTransaction);
            }

            return null;
        }

        public TransactionViewModel CreateViewModel(IInOutTransaction transaction)
        {
            var txInputs = transaction.Inputs;
            var txOutputs = transaction.Outputs;

            var ownInputs = txInputs
                .Where(i => IndexedOutputs.ContainsKey($"{i.Hash}:{i.Index}"))
                .Select(i => IndexedOutputs[$"{i.Hash}:{i.Index}"])
                .ToList();

            var ownOutputs = txOutputs
                .Where(o => IndexedOutputs.ContainsKey($"{o.TxId}:{o.Index}"))
                .ToList();

            if (ownInputs.Count == 0 && ownOutputs.Count > 0) // receive coins or swap refund or swap redeem
            {
                var receivedAmount = ownOutputs.Sum(o => o.Value) / (decimal)Currency.DigitsMultiplier;

                if (txInputs.Length == 1) // try to resolve one input
                {
                    // todo: try to resolve by swaps data firstly
                }

                return new TransactionViewModel
                {
                    Id = transaction.Id,
                    Amount = receivedAmount,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Type = TransactionType.Received,
                    Description = $"Received {receivedAmount.ToString(CultureInfo.InvariantCulture)} {Currency.Name}",
                    State = transaction.IsConfirmed()
                        ? TransactionState.Confirmed
                        : TransactionState.Unconfirmed,
                    Time = transaction.BlockInfo.FirstSeen,
                    Fee = transaction.BlockInfo.Fees / (decimal)Currency.DigitsMultiplier
                };
            }

            if (ownInputs.Count > 0 && ownOutputs.Count >= 0) // send coins or swap payment
            {
                var usedAmount = ownInputs.Sum(i => i.Value) / (decimal)Currency.DigitsMultiplier;
                var receivedAmount = ownOutputs.Sum(o => o.Value) / (decimal)Currency.DigitsMultiplier;
                var sentAmount = receivedAmount - usedAmount;

                // todo: try to resolve by swaps data firstly

                return new TransactionViewModel
                {
                    Id = transaction.Id,
                    Amount = sentAmount,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Type = TransactionType.Sent,
                    Description = $"Sent {Math.Abs(sentAmount).ToString(CultureInfo.InvariantCulture)} {Currency.Name}",
                    State = transaction.IsConfirmed()
                        ? TransactionState.Confirmed
                        : TransactionState.Unconfirmed,
                    Time = transaction.BlockInfo.FirstSeen,
                    Fee = transaction.BlockInfo.Fees / (decimal)Currency.DigitsMultiplier
                };
            }

            if (ownInputs.Count == 0 && ownOutputs.Count == 0) // unknown
            {
                return new TransactionViewModel
                {
                    Id = transaction.Id,
                    Amount = 0,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Type = TransactionType.Unknown,
                    Description = "Unknown transaction",
                    State = transaction.IsConfirmed()
                        ? TransactionState.Confirmed
                        : TransactionState.Unconfirmed,
                    Time = transaction.BlockInfo.FirstSeen,
                    Fee = transaction.BlockInfo.Fees / (decimal)Currency.DigitsMultiplier
                };
            }

            return null;
        }

        public TransactionViewModel CreateViewModel(IAddressBasedTransaction tx)
        {
            if (tx is EthereumTransaction ethTx)
            {
                var amount = EthereumAmountByType(ethTx);

                var type = EthereumTransactionType(ethTx);

                string description;

                switch (type)
                {
                    case TransactionType.Unknown:
                        description = "Unknown transaction";
                        break;
                    case TransactionType.Sent:
                        description = $"Sent {amount.ToString(CultureInfo.InvariantCulture)} {Currency.Name}";
                        break;
                    case TransactionType.Received:
                        description = $"Received {amount.ToString(CultureInfo.InvariantCulture)} {Currency.Name}";
                        break;
                    case TransactionType.Self:
                        description = $"Self transfer {amount.ToString(CultureInfo.InvariantCulture)} {Currency.Name}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                return new TransactionViewModel
                {
                    Id = tx.Id,
                    Amount = amount,
                    AmountFormat = CurrencyViewModel.CurrencyFormat,
                    CurrencyCode = CurrencyViewModel.CurrencyCode,
                    Type = type,
                    Description = description,
                    State = tx.IsConfirmed()
                        ? TransactionState.Confirmed
                        : TransactionState.Unconfirmed,
                    Time = tx.BlockInfo.FirstSeen,
                    Fee = tx.BlockInfo.Fees
                };
            }

            throw new NotSupportedException();
        }

        public TransactionType EthereumTransactionType(EthereumTransaction tx)
        {
            switch (tx.Type)
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

        public decimal EthereumAmountByType(EthereumTransaction tx)
        {
            var gas = tx.GasUsed != 0 ? tx.GasUsed : tx.GasLimit;

            switch (tx.Type)
            {
                case EthereumTransaction.UnknownTransaction:
                    return Ethereum.WeiToEth(tx.Amount + tx.GasPrice * gas);
                case EthereumTransaction.OutputTransaction:
                    return -Ethereum.WeiToEth(tx.Amount + tx.GasPrice * gas);
                case EthereumTransaction.InputTransaction:
                    return Ethereum.WeiToEth(tx.Amount);
                case EthereumTransaction.SelfTransaction:
                    return -Ethereum.WeiToEth(tx.GasPrice * gas);
                default:
                    return 0;
            }
        }
    }
}