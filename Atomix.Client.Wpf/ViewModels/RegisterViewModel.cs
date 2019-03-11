using System;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;

namespace Atomix.Client.Wpf.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        public IDialogViewer DialogViewer { get; set; }
        public BaseViewModel LoginViewModel { get; set; }

        private ICommand _backToLoginCommand;
        public ICommand BackToLoginCommand => _backToLoginCommand ?? (_backToLoginCommand = new Command(OnBackToLoginClick));

        public RegisterViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public RegisterViewModel(IDialogViewer dialogViewer)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        private void OnBackToLoginClick()
        {
            DialogViewer?.HideRegisterDialog(hideOverlay: false);
            DialogViewer?.ShowLoginDialog(LoginViewModel);
        }

        private void DesignerMode()
        {
        }
    }
}