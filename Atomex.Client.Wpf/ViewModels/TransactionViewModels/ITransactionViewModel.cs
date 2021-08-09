using System;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.ViewModels.Abstract;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public interface ITransactionViewModel : IExpandable
    {
        decimal Amount { get; set; }
        string AmountFormat { get; set; }
        string Description { get; set; }
        string Id { get; set; }
        DateTime LocalTime { get; }
        BlockchainTransactionState State { get; set; }
        DateTime Time { get; set; }
        IBlockchainTransaction Transaction { get; }
        BlockchainTransactionType Type { get; set; }
    }
}