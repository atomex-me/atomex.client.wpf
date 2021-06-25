using System;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.Client.Wpf.Common;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class EthereumTransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasPrice { get; set; }
        public decimal GasLimit { get; set; }
        public decimal GasUsed { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";

        public EthereumTransactionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public EthereumTransactionViewModel(EthereumTransaction tx, EthereumConfig ethereumConfig)
            : base(tx, ethereumConfig, GetAmount(tx), GetFee(tx))
        {
            From       = tx.From;
            To         = tx.To;
            GasPrice   = EthereumConfig.WeiToGwei((decimal) tx.GasPrice);
            GasLimit   = (decimal) tx.GasLimit;
            GasUsed    = (decimal) tx.GasUsed;
            Fee        = EthereumConfig.WeiToEth(tx.GasUsed * tx.GasPrice);
            IsInternal = tx.IsInternal;
        }

        private static decimal GetAmount(EthereumTransaction tx)
        {
            var result = 0m;
            
            if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                result += EthereumConfig.WeiToEth(tx.Amount);

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += -EthereumConfig.WeiToEth(tx.Amount + tx.GasUsed * tx.GasPrice);
           
            tx.InternalTxs?.ForEach(t => result += GetAmount(t));

            return result;
        }

        private static decimal GetFee(EthereumTransaction tx)
        {
            var result = 0m;

            if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                result += EthereumConfig.WeiToEth(tx.GasUsed * tx.GasPrice);

            tx.InternalTxs?.ForEach(t => result += GetFee(t));

            return result;
        }

        private void DesignerMode()
        {
            Id   = "0x1234567890abcdefgh1234567890abcdefgh";
            From = "0x1234567890abcdefgh1234567890abcdefgh";
            To   = "0x1234567890abcdefgh1234567890abcdefgh";
            Time = DateTime.UtcNow;
        }
    }
}