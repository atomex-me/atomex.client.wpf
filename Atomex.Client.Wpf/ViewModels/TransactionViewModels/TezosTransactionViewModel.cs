using System;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Common;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TezosTransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasLimit { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";

        public TezosTransactionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTransactionViewModel(TezosTransaction tx)
            : base(tx, GetAmount(tx))
        {
            From = tx.From;
            To = tx.To;
            GasLimit = tx.GasLimit;
            Fee = tx.Fee;
            IsInternal = tx.IsInternal;
        }

        private static decimal GetAmount(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += tx.Amount / tx.Currency.DigitsMultiplier;

            var includeFee = tx.Currency.Name == tx.Currency.FeeCurrencyName;
            var fee = includeFee ? tx.Fee : 0;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -(tx.Amount + fee) / tx.Currency.DigitsMultiplier;

            tx.InternalTxs?.ForEach(t => result += GetAmount(t));

            return result;
        }

        private void DesignerMode()
        {
            Id = "1234567890abcdefgh1234567890abcdefgh";
            From = "1234567890abcdefgh1234567890abcdefgh";
            To = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }
    }
}