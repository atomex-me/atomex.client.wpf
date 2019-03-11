using System;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Core.Entities;

namespace Atomix.Client.Wpf.ViewModels.SendViewModels
{
    public class SendViewModel : BaseViewModel
    {
        public IDialogViewer DialogViewer { get; }
        public Currency Currency { get; }

        public SendViewModel(IDialogViewer dialogViewer, Currency currency)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        public void ShowEdit()
        {
            Navigation.Navigate(
                uri: Navigation.EditAlias,
                context: new EditViewModel(DialogViewer, Currency));
        }
    }
}