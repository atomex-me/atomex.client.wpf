using System;
using System.Threading;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace Atomex.Client.Wpf.Controls
{
    public interface IDialogViewer
    {
        void ShowDialog(
            int dialogId,
            object dataContext,
            Action loaded = null,
            Action canceled = null,
            int defaultPageId = 0);

        void HideDialog(int dialogId);

        void HideAllDialogs();

        void PushPage(
            int dialogId,
            int pageId,
            object dataContext = null,
            Action closeAction = null);

        void PopPage(int dialogId);

        void Back(int dialogId);

        void ShowMessage(string title, string message);

        Task<MessageDialogResult> ShowMessageAsync(
            string title,
            string message,
            MessageDialogStyle style);

        Task ShowProgressAsync(
            string title,
            string message,
            Action canceled = null,
            CancellationToken cancellationToken = default);

        void HideProgress();
    }
}