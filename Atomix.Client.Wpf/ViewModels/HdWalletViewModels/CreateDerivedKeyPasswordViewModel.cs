using System;
using System.Security;
using System.Windows.Input;
using Atomix.Common;
using Atomix.Wallet;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Properties;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class CreateDerivedKeyPasswordViewModel : StageViewModel
    {
        public override event Action<object> OnNext;

        public MnemonicStageData MnemonicData { get; private set; }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private int _passwordScore;
        public int PasswordScore
        {
            get => _passwordScore;
            set { _passwordScore = value; OnPropertyChanged(nameof(PasswordScore)); }
        }

        private SecureString _password;
        public SecureString Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        private SecureString _passwordConfirmation;
        public SecureString PasswordConfirmation
        {
            get => _passwordConfirmation;
            set { _passwordConfirmation = value; OnPropertyChanged(nameof(PasswordConfirmation)); }
        }

        private ICommand _passwordChangedCommand;
        public ICommand PasswordChangedCommand => _passwordChangedCommand ?? (_passwordChangedCommand = new Command(PasswordChanged));

        private void PasswordChanged()
        {
            Warning = string.Empty;
            PasswordScore = (int) PasswordAdvisor.CheckStrength(Password);
        }

        public override void Initialize(object o)
        {
            MnemonicData = (MnemonicStageData)o;
        }

        public override void Back()
        {
            Warning = string.Empty;
            Password = null;
            PasswordConfirmation = null;
            PasswordScore = 0;

            base.Back();
        }

        public override void Next()
        {
            // password is optional
            if (Password != null && Password.Length > 0)
            {
                if (PasswordScore < (int)PasswordAdvisor.PasswordScore.Medium) {
                    Warning = Resources.CwvPasswordInsufficientComplexity;
                    return;
                }

                if (PasswordConfirmation != null && !Password.SecureEqual(PasswordConfirmation) || PasswordConfirmation == null) {
                    Warning = Resources.CwvPasswordsDoNotMatch;
                    return;
                }
            }

            var wallet = new HdWallet(MnemonicData.Mnemonic, MnemonicData.Language, Password) {
                PathToWallet = MnemonicData.PathToWallet
            };

            Password = null;
            PasswordConfirmation = null;
            PasswordScore = 0;

            OnNext?.Invoke(wallet);
        }
    }
}