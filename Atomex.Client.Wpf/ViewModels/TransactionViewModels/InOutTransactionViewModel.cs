using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Controls;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class InOutTransactionViewModel : TransactionViewModel
    {
        public bool HasSelfInputs { get; }
        public bool HasSelfOutputs { get; }
        public IList<string> SelfInputAddresses { get; }
        public IList<string> SelfOutputAddresses { get; }

        public InOutTransactionViewModel(
            IInOutTransaction tx,
            IDictionary<string, ITxOutput> indexedOutputs)
        {
            var currencyViewModel = CurrencyViewModelCreator.CreateViewModel(tx.Currency, false);

            var txInputs = tx.Inputs;
            var txOutputs = tx.Outputs;

            var ownInputs = txInputs
                .Where(i => indexedOutputs.ContainsKey($"{i.Hash}:{i.Index}"))
                .Select(i => indexedOutputs[$"{i.Hash}:{i.Index}"])
                .ToList();

            SelfInputAddresses = ownInputs.Count > 0
                ? ownInputs
                    .Select(o => o.DestinationAddress(tx.Currency))
                    .Distinct()
                    .ToList()
                : new List<string>();

            var ownOutputs = txOutputs
                .Where(o => indexedOutputs.ContainsKey($"{o.TxId}:{o.Index}"))
                .ToList();

            SelfOutputAddresses = ownOutputs.Count > 0
                ? ownOutputs
                    .Select(o => o.DestinationAddress(tx.Currency))
                    .Distinct()
                    .ToList()
                : new List<string>();

            Currency = tx.Currency;
            Id = tx.Id;
            AmountFormat = currencyViewModel.CurrencyFormat;
            CurrencyCode = currencyViewModel.CurrencyCode;
            State = tx.IsConfirmed()
                ? TransactionState.Confirmed
                : TransactionState.Unconfirmed;
            Time = tx.BlockInfo.FirstSeen;
            Fee = tx.BlockInfo.Fees / (decimal)tx.Currency.DigitsMultiplier;

            if (ownInputs.Count == 0 && ownOutputs.Count > 0) // receive coins or swap refund or swap redeem
            {
                var receivedAmount = ownOutputs.Sum(o => o.Value) / (decimal)tx.Currency.DigitsMultiplier;

                if (txInputs.Length == 1) // try to resolve one input
                {
                    // todo: try to resolve by swaps data firstly
                }

                Amount = receivedAmount;
                Type = TransactionType.Received;
                Description = $"Received {receivedAmount.ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }

            if (ownInputs.Count > 0 && ownOutputs.Count >= 0) // send coins or swap payment
            {
                var usedAmount = ownInputs.Sum(i => i.Value) / (decimal)tx.Currency.DigitsMultiplier;
                var receivedAmount = ownOutputs.Sum(o => o.Value) / (decimal)tx.Currency.DigitsMultiplier;
                var sentAmount = receivedAmount - usedAmount;

                // todo: try to resolve by swaps data firstly

                Amount = sentAmount;
                Type = TransactionType.Sent;
                Description = $"Sent {Math.Abs(sentAmount).ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
            }

            if (ownInputs.Count == 0 && ownOutputs.Count == 0) // unknown
            {
                Type = TransactionType.Unknown;
                Description = "Unknown transaction";
            }
        }
    }
}