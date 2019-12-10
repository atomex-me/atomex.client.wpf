using System;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace Atomex.Client.Wpf.Controls
{
    public interface IDialogViewer
    {
        void HideAllDialogs();

        void ShowStartDialog(object dataContext);
        void HideStartDialog();

        void ShowCreateWalletDialog(object dataContext);
        void HideCreateWalletDialog();

        void ShowSendDialog(object dataContext, Action dialogLoaded = null);
        void HideSendDialog();

        void ShowDelegateDialog(object dataContext, Action dialogLoaded = null);
        void HideDelegateDialog();

        void ShowConversionConfirmationDialog(object dataContext, Action dialogLoaded = null);
        void HideConversionConfirmationDialog();

        void ShowReceiveDialog(object dataContext);
        void HideReceiveDialog();

        void ShowUnlockDialog(object dataContext, EventHandler canceled = null);
        void HideUnlockDialog();

        void ShowMyWalletsDialog(object dataContext);
        void HideMyWalletsDialog();

        void ShowMessage(string title, string message);
        Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style);
    }
}