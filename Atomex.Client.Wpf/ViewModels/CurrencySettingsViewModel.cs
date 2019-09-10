using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;
using Atomex.Common;

namespace Atomex.Client.Wpf.ViewModels
{
    public class CurrencySettingsViewModel : BaseViewModel
    {

        public CurrencyViewModel CurrencyViewModel { get; set; }

        public CurrencySettingsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        private void DesignerMode()
        {
            CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(
                currency: DesignTime.Currencies.Get<Bitcoin>(),
                subscribeToUpdates: false);
        }
    }
}