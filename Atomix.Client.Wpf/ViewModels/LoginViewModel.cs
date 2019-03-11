using System;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;

namespace Atomix.Client.Wpf.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public IDialogViewer DialogViewer { get; set; }
        public BaseViewModel RegisterViewModel { get; set; }

        private ICommand _registerCommand;
        public ICommand RegisterCommand => _registerCommand ?? (_registerCommand = new Command(OnRegisterClick));

        private ICommand _forgotPasswordCommand;
        public ICommand ForgotPasswordCommand => _forgotPasswordCommand ?? (_forgotPasswordCommand = new Command(() => { }));

        private ICommand _logInCommand;
        public ICommand LogInCommand => _logInCommand ?? (_logInCommand = new Command(() => { }));

        private ICommand _anonymousCommand;
        public ICommand AnonymousCommand => _anonymousCommand ?? (_anonymousCommand = new Command(() => { }));

        public LoginViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public LoginViewModel(IDialogViewer dialogViewer)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        private void OnRegisterClick()
        {
            DialogViewer?.HideLoginDialog(hideOverlay: false);
            DialogViewer?.ShowRegisterDialog(RegisterViewModel);
        }

        private void DesignerMode()
        {

        }
    }
}