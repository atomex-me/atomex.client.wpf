using Atomex.Client.Wpf.Controls;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class FA2SendViewModel : Fa12SendViewModel
    {
        public FA2SendViewModel()
            : base()
        {
        }

        public FA2SendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer, currency)
        {
        }
    }
}

