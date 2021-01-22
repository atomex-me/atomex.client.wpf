using System;
using System.Security;
using System.Windows.Input;

using Serilog;

using Atomex.Common;
using Atomex.Wallet;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Properties;

namespace Atomex.Client.Wpf.ViewModels.CreateWalletViewModels
{
    public class CreateStoragePasswordViewModel : StepViewModel
    {
        private IAtomexApp App { get; }
        private HdWallet Wallet { get; set; }

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
        public ICommand PasswordChangedCommand => _passwordChangedCommand ??= new Command(PasswordChanged);

        public CreateStoragePasswordViewModel(
            IAtomexApp app)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
        }

        private void PasswordChanged()
        {
            Warning = string.Empty;
            PasswordScore = (int)PasswordAdvisor.CheckStrength(Password);
        }

        public override void Initialize(object o)
        {
            Wallet = (HdWallet) o;
        }

        public override void Back()
        {
            Warning = string.Empty;
            Password = null;
            PasswordConfirmation = null;
            PasswordScore = 0;

            base.Back();
        }

        public override async void Next()
        {
            if (PasswordScore < (int)PasswordAdvisor.PasswordScore.Medium) {
                Warning = Resources.CwvPasswordInsufficientComplexity;
                return;
            }

            if (Password != null &&
                PasswordConfirmation != null && 
                !Password.SecureEqual(PasswordConfirmation) || PasswordConfirmation == null) {
                Warning = Resources.CwvPasswordsDoNotMatch;
                return;
            }

            try
            {
                RaiseProgressBarShow();

                await Wallet.EncryptAsync(Password);

                Wallet.SaveToFile(Wallet.PathToWallet, Password);

                var account = new Account(
                    wallet: Wallet,
                    password: Password,
                    currenciesProvider: App.CurrenciesProvider,
                    symbolsProvider: App.SymbolsProvider,
                    clientType: ClientType.Wpf);

                Password = null;
                PasswordConfirmation = null;
                PasswordScore = 0;

                RaiseOnNext(account);
            }
            catch (Exception e)
            {
                // todo: warning
                Log.Error(e, "Create storage password error");
            }
            finally
            {
                RaiseProgressBarHide();
            }
        }
    }
}