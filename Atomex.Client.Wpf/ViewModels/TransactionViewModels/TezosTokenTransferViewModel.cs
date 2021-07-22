using System;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using Atomex.Blockchain.Abstract;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Common;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TezosTokenTransferViewModel : BaseViewModel, ITransactionViewModel
    {
        public const int MaxAmountDecimals = 9;

        private readonly TezosConfig _tezosConfig;


        public IBlockchainTransaction Transaction { get; }
        public string Id { get; set; }
        public BlockchainTransactionState State { get; set; }
        public BlockchainTransactionType Type { get; set; }

        public string From { get; set; }
        public string To { get; set; }

        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }

        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri => $"{_tezosConfig.TxExplorerUri}{Id}";
        public string FromExplorerUri => $"{_tezosConfig.AddressExplorerUri}{From}";
        public string ToExplorerUri => $"{_tezosConfig.AddressExplorerUri}{To}";


        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        public TezosTokenTransferViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokenTransferViewModel(TokenTransfer tx, TezosConfig tezosConfig)
        {
            _tezosConfig = tezosConfig;

            Transaction  = tx ?? throw new ArgumentNullException(nameof(tx));
            Id           = tx.Hash;
            State        = Transaction.State;
            Type         = Transaction.Type;
            From         = tx.From;
            To           = tx.To;
            Amount       = GetAmount(tx);
            AmountFormat = $"F{Math.Min(tx.Token.Decimals, MaxAmountDecimals)}";
            CurrencyCode = tx.Token.Symbol;
            Time         = tx.CreationTime ?? DateTime.UtcNow;

            Description = TransactionViewModel.GetDescription(
                type: tx.Type,
                amount: Amount,
                netAmount: Amount,
                amountDigits: tx.Token.Decimals,
                currencyCode: tx.Token.Symbol);
        }

        private ICommand _openTxInExplorerCommand;
        public ICommand OpenTxInExplorerCommand => _openTxInExplorerCommand ??= new RelayCommand<string>((id) =>
        {
            if (Uri.TryCreate($"{_tezosConfig.TxExplorerUri}{id}", UriKind.Absolute, out var uri))
                Process.Start(uri.ToString());
            else
                Log.Error("Invalid uri for transaction explorer");
        });

        private ICommand _openAddressInExplorerCommand;
        public ICommand OpenAddressInExplorerCommand => _openAddressInExplorerCommand ??= new RelayCommand<string>((address) =>
        {
            if (Uri.TryCreate($"{_tezosConfig.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                Process.Start(uri.ToString());
            else
                Log.Error("Invalid uri for address explorer");
        });

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new RelayCommand<string>((s) =>
        {
            try
            {
                Clipboard.SetText(s);
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        });

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