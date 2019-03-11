using System;
using NBitcoin;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class MnemonicStageData
    {
        public string Mnemonic { get; set; }
        public Wordlist Language { get; set; }
        public string PathToWallet { get; set; }
    }

    public class StageViewModel : BaseViewModel
    {
        public virtual event Action<object> OnBack;
        public virtual event Action<object> OnNext;
        public virtual event Action OnProgressShow;
        public virtual event Action OnProgressHide;

        public virtual void Initialize(object o) { }
        public virtual void Back() => OnBack?.Invoke(null);
        public virtual void Next() => OnNext?.Invoke(null);
    }
}