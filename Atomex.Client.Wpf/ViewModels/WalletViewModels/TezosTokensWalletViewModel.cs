using System.Collections.ObjectModel;
using System.Windows.Media;

using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public class TezosTokenViewModel
    {
        public string Name { get; set; }
        public string IconUri { get; set; }
        public string Contract { get; set; }
        public string Type { get; set; }
        public decimal Balance { get; set; }
    }

    public class TezosTokensWalletViewModel : BaseViewModel, IWalletViewModel
    {
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }

        public string Header => "Tezos Tokens";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
             {
                _isSelected = value;

                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(OpacityMask));
            }
        }

        public Brush Background => IsSelected
            ? TezosCurrencyViewModel.DefaultIconBrush
            : TezosCurrencyViewModel.DefaultUnselectedIconBrush;

        public Brush OpacityMask => IsSelected
            ? null
            : TezosCurrencyViewModel.DefaultIconMaskBrush;

        public TezosTokensWalletViewModel()
        {
        }

        public TezosTokensWalletViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel,
            CurrencyConfig currency)
        {
        }

        protected void DesignerMode()
        {

        }
    }
}