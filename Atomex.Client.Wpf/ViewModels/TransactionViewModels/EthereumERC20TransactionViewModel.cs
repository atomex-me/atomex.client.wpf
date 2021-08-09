using System;

using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Ethereum;
using Atomex.Client.Wpf.Common;
using Atomex.EthereumTokens;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class EthereumERC20TransactionViewModel : TransactionViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public decimal GasPrice { get; set; }
        public decimal GasLimit { get; set; }
        public decimal GasUsed { get; set; }
        public bool IsInternal { get; set; }
        public string FromExplorerUri => $"{Currency.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{Currency.AddressExplorerUri}{To}";

        public EthereumERC20TransactionViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public EthereumERC20TransactionViewModel(
            EthereumTransaction tx,
            Erc20Config erc20Config)
            : base(tx, erc20Config, GetAmount(tx, erc20Config), 0)
        {
            From       = tx.From;
            To         = tx.To;
            GasPrice   = EthereumConfig.WeiToGwei((decimal)tx.GasPrice);
            GasLimit   = (decimal)tx.GasLimit;
            GasUsed    = (decimal)tx.GasUsed;
            IsInternal = tx.IsInternal;
        }

        public static decimal GetAmount(
            EthereumTransaction tx,
            Erc20Config erc20Config)
        {
            var result = 0m;
            
            if (tx.Type.HasFlag(BlockchainTransactionType.SwapRedeem) ||
                tx.Type.HasFlag(BlockchainTransactionType.SwapRefund))
                result += erc20Config.TokenDigitsToTokens(tx.Amount);
            else
            {
                if (tx.Type.HasFlag(BlockchainTransactionType.Input))
                    result += erc20Config.TokenDigitsToTokens(tx.Amount);
                if (tx.Type.HasFlag(BlockchainTransactionType.Output))
                    result += -erc20Config.TokenDigitsToTokens(tx.Amount);
            }

            tx.InternalTxs?.ForEach(t => result += GetAmount(t, erc20Config));

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