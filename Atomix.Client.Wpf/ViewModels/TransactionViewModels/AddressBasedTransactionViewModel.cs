using System;
using System.Globalization;
using Atomix.Blockchain;
using Atomix.Blockchain.Abstract;

namespace Atomix.Client.Wpf.ViewModels.TransactionViewModels
{
    public abstract class AddressBasedTransactionViewModel : TransactionViewModel
    {
        public AddressBasedTransactionViewModel(
            IAddressBasedTransaction tx)
        {
            var currencyViewModel = CurrencyViewModelCreator.CreateViewModel(tx.Currency, false);
            var amount = GetAmount(tx);
            var type = GetType(tx);

            string description;

            switch (type)
            {
                case TransactionType.Unknown:
                    description = "Unknown transaction";
                    break;
                case TransactionType.Sent:
                    description = $"Sent {amount.ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
                    break;
                case TransactionType.Received:
                    description = $"Received {amount.ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
                    break;
                case TransactionType.Self:
                    description = $"Self transfer {amount.ToString(CultureInfo.InvariantCulture)} {tx.Currency.Name}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Id = tx.Id;
            Amount = amount;
            AmountFormat = currencyViewModel.CurrencyFormat;
            CurrencyCode = currencyViewModel.CurrencyCode;
            Type = type;
            Description = description;
            State = tx.IsConfirmed()
                ? TransactionState.Confirmed
                : TransactionState.Unconfirmed;
            Time = tx.BlockInfo.FirstSeen;
            Fee = tx.BlockInfo.Fees;
        }

        public abstract decimal GetAmount(IAddressBasedTransaction tx);
        public abstract TransactionType GetType(IAddressBasedTransaction tx);
    }
}