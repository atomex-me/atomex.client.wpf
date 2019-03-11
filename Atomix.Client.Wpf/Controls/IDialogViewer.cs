using System;
using System.Threading.Tasks;

namespace Atomix.Client.Wpf.Controls
{
    public interface IDialogViewer
    {
        void ShowLoginDialog(object dataContext);
        void HideLoginDialog(bool hideOverlay = true);

        void ShowRegisterDialog(object dataContext);
        void HideRegisterDialog(bool hideOverlay = true);

        void ShowStartDialog(object dataContext);
        void HideStartDialog(bool hideOverlay = true);

        void ShowCreateWalletDialog(object dataContext);
        void HideCreateWalletDialog(bool hideOverlay = true);

        void ShowSendDialog(object dataContext, Action dialogLoaded = null);
        void HideSendDialog(bool hideOverlay = true);

        void ShowReceiveDialog(object dataContext);
        void HideReceiveDialog(bool hideOverlay = true);

        Task ShowUnlockDialogAsync(object dataContext);
        void HideUnlockDialog();

        void ShowMessage(string title, string message);
    }
}