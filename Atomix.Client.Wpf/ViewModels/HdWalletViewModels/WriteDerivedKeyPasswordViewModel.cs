using System;
using System.Security;
using Atomix.Wallet;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class WriteDerivedKeyPasswordViewModel : StageViewModel
    {
        public override event Action<object> OnNext;

        private MnemonicStageData MnemonicData { get; set; }

        private SecureString _password;
        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public override void Initialize(object o)
        {
            MnemonicData = (MnemonicStageData) o;
        }

        public override void Back()
        {
            Password = null;
            base.Back();
        }

        public override void Next()
        {
            var wallet = new HdWallet(MnemonicData.Mnemonic, MnemonicData.Language, Password) {
                PathToWallet = MnemonicData.PathToWallet
            };

            Password = null;

            OnNext?.Invoke(wallet);
        }
    }
}