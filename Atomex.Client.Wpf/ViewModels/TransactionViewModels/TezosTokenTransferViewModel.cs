using System;
using System.Numerics;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Common;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TezosTokenTransferViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";

        public TezosTokenTransferViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokenTransferViewModel(TokenTransfer tx, CurrencyConfig config)
            : base(tx, config, GetAmount(tx), 0)
        {
            Id   = tx.Hash;
            From = tx.From;
            To   = tx.To;
        }

        private static decimal GetAmount(TokenTransfer tx)
        {
            if (!decimal.TryParse(tx.Amount, out var amount))
                return 0;

            var sign = tx.Type.HasFlag(BlockchainTransactionType.Input)
                ? 1
                : -1;

            return sign * amount / (decimal)BigInteger.Pow(10, tx.Token.Decimals);
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