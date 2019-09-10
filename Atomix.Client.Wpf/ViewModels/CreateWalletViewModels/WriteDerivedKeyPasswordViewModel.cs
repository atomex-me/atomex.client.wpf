using System.Security;
using Atomix.Wallet;

namespace Atomix.Client.Wpf.ViewModels.CreateWalletViewModels
{
    public class WriteDerivedKeyPasswordViewModel : StepViewModel
    {
        private StepData StepData { get; set; }

        private SecureString _password;
        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public override void Initialize(
            object o)
        {
            StepData = (StepData) o;
        }

        public override void Back()
        {
            Password = null;

            base.Back();
        }

        public override void Next()
        {
            var wallet = new HdWallet(
                mnemonic: StepData.Mnemonic,
                wordList: StepData.Language,
                passPhrase: Password,
                network: StepData.Network) {
                PathToWallet = StepData.PathToWallet
            };

            Password = null;

            RaiseOnNext(wallet);
        }
    }
}