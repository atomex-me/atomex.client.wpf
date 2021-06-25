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

        public TezosTransactionViewModel(TezosTransaction tx, TezosConfig tezosConfig)
            : base(tx, tezosConfig, GetAmount(tx, tezosConfig), GetFee(tx))
        {
            From       = tx.From;
            To         = tx.To;
            GasLimit   = tx.GasLimit;
            Fee        = TezosConfig.MtzToTz(tx.Fee);
            IsInternal = tx.IsInternal;
        }

        private static decimal GetAmount(TezosTransaction tx, TezosConfig tezosConfig)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += tx.Amount / tezosConfig.DigitsMultiplier;

            var includeFee = tezosConfig.Name == tezosConfig.FeeCurrencyName;
            var fee = includeFee ? tx.Fee : 0;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -(tx.Amount + fee) / tezosConfig.DigitsMultiplier;

            tx.InternalTxs?.ForEach(t => result += GetAmount(t, tezosConfig));

            return result;
        }

        private static decimal GetFee(TezosTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += TezosConfig.MtzToTz(tx.Fee);

            tx.InternalTxs?.ForEach(t => result += GetFee(t));

            return result;
        }
               
        private void DesignerMode()
        {
            Id   = "1234567890abcdefgh1234567890abcdefgh";
            From = "1234567890abcdefgh1234567890abcdefgh";
            To   = "1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }
    }
}