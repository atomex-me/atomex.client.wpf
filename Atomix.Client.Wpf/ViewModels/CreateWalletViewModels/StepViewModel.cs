using System;
using NBitcoin;
using Network = Atomix.Core.Network;

namespace Atomix.Client.Wpf.ViewModels.CreateWalletViewModels
{
    public class StepData
    {
        public Network Network { get; set; }
        public string PathToWallet { get; set; }
        public string Mnemonic { get; set; }
        public Wordlist Language { get; set; }
    }

    public class StepViewModel : BaseViewModel
    {
        public event Action<object> OnBack;
        public event Action<object> OnNext;
        public event Action ProgressBarShow;
        public event Action ProgressBarHide;

        private int _step;
        public int Step
        {
            get => _step;
            set { _step = value; OnPropertyChanged(nameof(Step)); }
        }

        public virtual void Initialize(
            object o)
        {
        }

        public virtual void Back() => OnBack?.Invoke(null);

        public virtual void Next() => OnNext?.Invoke(null);

        protected void RaiseOnBack(
            object arg)
        {
            OnBack?.Invoke(arg);
        }

        protected void RaiseOnNext(
            object arg)
        {
            OnNext?.Invoke(arg);
        }

        protected void RaiseProgressBarShow()
        {
            ProgressBarShow?.Invoke();
        }

        protected void RaiseProgressBarHide()
        {
            ProgressBarHide?.Invoke();
        }
    }
}