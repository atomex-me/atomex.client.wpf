using System;
using System.Collections.ObjectModel;
using System.Linq;
using Atomex.Subsystems;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Client.Wpf.ViewModels.WalletViewModels;

namespace Atomex.Client.Wpf.ViewModels
{
    public class WalletsViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        private IMenuSelector MenuSelector { get; }
        private IConversionViewModel ConversionViewModel { get; }

        private ObservableCollection<WalletViewModel> _wallets;
        public ObservableCollection<WalletViewModel> Wallets
        {
            get => _wallets;
            set { _wallets = value; OnPropertyChanged(nameof(Wallets)); }
        }

        private WalletViewModel _selected;
        public WalletViewModel Selected
        {
            get => _selected;
            set
            {
                if (_selected != null)
                    _selected.IsSelected = false;

                _selected = value;

                if (_selected != null)
                    _selected.IsSelected = true;

                OnPropertyChanged(nameof(Selected));
            }
        }

        public WalletsViewModel()
        {
 #if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public WalletsViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            MenuSelector = menuSelector ?? throw new ArgumentNullException(nameof(menuSelector));
            ConversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));

            SubscribeToServices();
        }

        private void SubscribeToServices()
        {
            App.TerminalChanged += OnTerminalChangedEventHandler;
        }

        private void OnTerminalChangedEventHandler(object sender, TerminalChangedEventArgs e)
        {
            Wallets = e.Terminal?.Account != null
                ? new ObservableCollection<WalletViewModel>(
                    e.Terminal.Account.Currencies.Select(currency => WalletViewModelCreator.CreateViewModel(
                        app: App,
                        dialogViewer: DialogViewer,
                        menuSelector: MenuSelector,
                        conversionViewModel: ConversionViewModel,
                        currency: currency)))
                : new ObservableCollection<WalletViewModel>();

            Selected = Wallets.FirstOrDefault();
        }

        private void DesignerMode()
        {
            var currencies = DesignTime.Currencies;

            Wallets = new ObservableCollection<WalletViewModel>
            {
                new WalletViewModel {CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[0], subscribeToUpdates: false)},
                new WalletViewModel {CurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(currencies[1], subscribeToUpdates: false)}
            };
        }
    }
}