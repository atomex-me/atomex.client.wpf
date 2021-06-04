using Atomex.Client.Wpf.Controls;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class TezosSendViewModel : SendViewModel
    {
        public TezosSendViewModel()
            : base()
        {
        }

        public TezosSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            CurrencyConfig currency)
            : base(app, dialogViewer, currency)
        {
        }
    }
}