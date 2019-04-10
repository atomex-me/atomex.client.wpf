using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels
{
    public class UnlockViewModel : BaseViewModel
    {
        public event EventHandler Unlocked;
        public event EventHandler<ErrorEventArgs> Error; 

        public SecureString Password { get; set; }
        public string WalletName { get; set; }

        private bool _inProgress;
        public bool InProgress
        {
            get => _inProgress;
            set { _inProgress = value; OnPropertyChanged(nameof(InProgress)); }
        }

        private bool _invalidPassword;
        public bool InvalidPassword
        {
            get => _invalidPassword;
            set { _invalidPassword = value; OnPropertyChanged(nameof(InvalidPassword)); }
        }

        private ICommand _unlockCommand;
        public ICommand UnlockCommand => _unlockCommand ?? (_unlockCommand = new Command(OnUnlockClick));

        private readonly Func<SecureString, Task> _unlockAction;

        public UnlockViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public UnlockViewModel(string walletName, Func<SecureString, Task> unlockAction)
        {
            WalletName = $"\"{walletName}\"";
            _unlockAction = unlockAction;
        }

        private async void OnUnlockClick()
        {
            InvalidPassword = false;
            InProgress = true;

            try
            {
                await _unlockAction(Password);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unlock error");

                InvalidPassword = true;
                InProgress = false;

                Error?.Invoke(this, new ErrorEventArgs(e));
                return;
            }

            InProgress = false;
            Password = null;

            Unlocked?.Invoke(this, EventArgs.Empty);
        }

        private void DesignerMode()
        {
            InProgress = true;
        }
    }
}