using System;
using Atomix.Blockchain;
using Atomix.Client.Wpf.ViewModels.Abstract;

namespace Atomix.Client.Wpf.ViewModels
{
    public class TransactionViewModel : BaseViewModel, IExpandable
    {
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

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
        }
    }
}