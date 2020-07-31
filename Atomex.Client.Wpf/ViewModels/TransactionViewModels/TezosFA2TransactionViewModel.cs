using Atomex.Blockchain.Tezos;

namespace Atomex.Client.Wpf.ViewModels.TransactionViewModels
{
    public class TezosFA2TransactionViewModel : TezosFA12TransactionViewModel
    {
        public TezosFA2TransactionViewModel()
            : base()
        {
        }

        public TezosFA2TransactionViewModel(TezosTransaction tx)
            : base(tx)
        {
        }
    }
}

