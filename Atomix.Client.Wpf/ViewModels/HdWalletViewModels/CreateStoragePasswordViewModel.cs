using System;
using System.Security;
using System.Windows.Input;
using Atomix.Common;
using Atomix.Wallet;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Properties;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class CreateStoragePasswordViewModel : StageViewModel
    {
        public override event Action<object> OnNext;
        public override event Action OnProgressShow;
        public override event Action OnProgressHide;

        public HdWallet Wallet { get; private set; }

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

            if (Password != null && PasswordConfirmation != null && !Password.SecureEqual(PasswordConfirmation) || PasswordConfirmation == null) {
                Warning = Resources.CwvPasswordsDoNotMatch;
                return;
            }

            try
            {
                OnProgressShow?.Invoke();

                await Wallet.EncryptAsync(Password);

                Wallet.SaveToFile(Wallet.PathToWallet, Password);

                var account = new Account(Wallet, Password);

                Password = null;
                PasswordConfirmation = null;
                PasswordScore = 0;

                OnNext?.Invoke(account);
            }
            catch (Exception e)
            {
                // todo: warning
                Log.Error(e, "Create storage password error");
            }
            finally
            {
                OnProgressHide?.Invoke();
            }
        }
    }
}