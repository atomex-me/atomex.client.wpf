using Atomex.Client.Wpf.Controls;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class NYXSendViewModel : Fa12SendViewModel
    {
        public NYXSendViewModel()
            : base()
        {
        }

        public NYXSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            CurrencyConfig currency)
            : base(app, dialogViewer, currency)
        {
        }
    }
}

