using Atomex.Client.Wpf.Controls;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class BitcoinBasedSendViewModel : SendViewModel
    {
        public BitcoinBasedSendViewModel()
            : base()
        {
        }

        public BitcoinBasedSendViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Currency currency)
            : base(app, dialogViewer, currency)
        {
        }
    }
}