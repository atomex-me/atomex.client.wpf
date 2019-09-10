using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Client.Wpf.ViewModels.TransactionViewModels;
using Atomix.Common;

namespace Atomix.Client.Wpf.ViewModels
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