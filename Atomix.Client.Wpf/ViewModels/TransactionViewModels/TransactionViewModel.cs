using System;
using System.Diagnostics;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Core.Entities;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TransactionViewModel : BaseViewModel, IExpandable
    {
        public Currency Currency { get; set; }
        public string Id { get; set; }
        public TransactionType Type { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormat { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Fee { get; set; }
        public TransactionState State { get; set; }
        public DateTime Time { get; set; }
        public DateTime LocalTime => Time.ToLocalTime();
        public string TxExplorerUri => $"{Currency.TxExplorerUri}{Id}";

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }

        private ICommand _txIdCommand;
        public ICommand TxIdCommand => _txIdCommand ?? (_txIdCommand = new Command(() =>
        {
            if (Uri.TryCreate(TxExplorerUri, UriKind.Absolute, out var uri))
            {
                Process.Start(uri.ToString());
            }
            else
            {
                Log.Error("Invalid uri for transaction explorer");
            }
        }));
    }
}