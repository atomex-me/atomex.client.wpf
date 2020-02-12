using System.Security;
using System.Windows.Input;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Properties;

namespace Atomex.Client.Wpf.ViewModels.CreateWalletViewModels
{
    public class CreateDerivedKeyPasswordViewModel : StepViewModel
    {
        private StepData StepData { get; set; }

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

        public override void Initialize(
            object o)
        {
            StepData = (StepData)o;
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

            var wallet = new HdWallet(
                mnemonic: StepData.Mnemonic,
                wordList: StepData.Language,
                passPhrase: Password,
                network: StepData.Network)
            {
                PathToWallet = StepData.PathToWallet
            };

            Password = null;
            PasswordConfirmation = null;
            PasswordScore = 0;

            RaiseOnNext(wallet);
        }
    }
}