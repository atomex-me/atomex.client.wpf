using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

using Helpers;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.Subsystems;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class CurrencyItemViewModel : BaseViewModel
    {
        private readonly CurrencyViewModel _currencyViewModel;

        public string Header => _currencyViewModel.Header;
        public CurrencyConfig Currency => _currencyViewModel.Currency;

        public Brush Background => IsSelected
            ? _currencyViewModel.IconBrush
            : _currencyViewModel.UnselectedIconBrush;

        public Brush OpacityMask => IsSelected
            ? _currencyViewModel.IconBrush is ImageBrush
                ? null
                : _currencyViewModel.IconMaskBrush
            : _currencyViewModel.IconMaskBrush;

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

        public CurrencyItemViewModel(CurrencyConfig currency)
        {
            _currencyViewModel = CurrencyViewModelCreator.CreateViewModel(currency);
        }
    }

    public class BuyWithCardViewModel : BaseViewModel
    {
        private IAccount _account;

        public bool IsReady => !IsLoading && Url != null;
        public bool IsLoading { get; set; }
        public string Url { get; set;}

        public List<CurrencyItemViewModel> Currencies { get; set; }

        private CurrencyItemViewModel _selected;
        public CurrencyItemViewModel Selected
        {
            get => _selected;
            set
            {
                var previousValue = _selected;

                _selected = value;

                if (_selected != null && previousValue?.Currency.Name != _selected.Currency.Name)
                {
                    if (previousValue != null)
                        previousValue.IsSelected = false;

                    _selected.IsSelected = true;

                    Open(currency: _selected.Currency.Name,
                        address: GetDefaultAddress(_selected.Currency.Name),
                        network: _account.Network);
                }
            }
        }

        private ICommand _navigationCompletedCommand;
        public ICommand NavigationCompletedCommand => _navigationCompletedCommand ??= new Command(() =>
        {
            IsLoading = false;

            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(IsLoading));
        });

        private ICommand _navigatingStartingCommand;
        public ICommand NavigatingStartingCommand => _navigatingStartingCommand ??= new Command(() =>
        {
            IsLoading = true;

            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(IsLoading));
        });

        public BuyWithCardViewModel(IAtomexApp app)
        {
            app.TerminalChanged += TerminalChanged;
        }

        private void TerminalChanged(object sender, TerminalChangedEventArgs e)
        {
            _account = e.Terminal?.Account;

            if (_account == null)
                return;

            if (Currencies != null)
                Currencies.Clear();

            Currencies = new List<CurrencyItemViewModel>
            {
                new CurrencyItemViewModel(_account.Currencies.GetByName("BTC")),
                new CurrencyItemViewModel(_account.Currencies.GetByName("ETH")),
                new CurrencyItemViewModel(_account.Currencies.GetByName("XTZ"))
            };

            OnPropertyChanged(nameof(Currencies));
        }

        public void Open(string currency, string address, Network network)
        {
            var baseUri = network == Network.MainNet
                ? "https://widget.wert.io/atomex"
                : "https://sandbox.wert.io/01F298K3HP4DY326AH1NS3MM3M";

            Url = $"{baseUri}/widget" +
                $"?theme=dark" +
                $"&commodity={currency}" +
                $"&address={address}" +
                $"&click_id=user:{_account.GetUserId()}/network:{network}";

            OnPropertyChanged(nameof(Url));
            OnPropertyChanged(nameof(IsReady));
        }

        public string GetDefaultAddress(string currency)
        {
            if (currency == "ETH" || currency == "XTZ")
            {
                var account = _account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(currency);

                var activeTokenAddresses = account
                    .GetUnspentTokenAddressesAsync()
                    .WaitForResult()
                    .ToList();

                var activeAddresses = _account
                    .GetUnspentAddressesAsync(currency)
                    .WaitForResult()
                    .ToList();

                activeTokenAddresses
                    .ForEach(a => a.Balance = activeAddresses
                        .Find(b => b.Address == a.Address)?.Balance ?? 0m);

                activeAddresses = activeAddresses
                    .Where(a => activeTokenAddresses
                        .FirstOrDefault(b => b.Address == a.Address) == null)
                    .ToList();

                var receiveAddress = activeTokenAddresses
                    .Concat(activeAddresses)
                    .OrderByDescending(w => w.AvailableBalance())
                    .FirstOrDefault(w => w.HasActivity);

                if (receiveAddress != null)
                    return receiveAddress.Address;
            }

            return _account
                .GetFreeExternalAddressAsync(currency)
                .WaitForResult()
                .Address;
        }
    }
}