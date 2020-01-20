using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Atomex.Core;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Common;
using QRCoder;
using System;
using System.Windows.Input;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ReceiveViewModel : BaseViewModel
    {
        private const int PixelsPerModule = 20;

        private IAtomexApp App { get; }

        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        private Currency _currency;
        public Currency Currency
        {
            get => _currency;
            set
            {
                _currency = value;
                OnPropertyChanged(nameof(Currency));
#if DEBUG
                if (!Env.IsInDesignerMode())
                {
#endif
                    var activeAddresses = App.Account
                        .GetUnspentAddressesAsync(_currency.Name)
                        .WaitForResult();

                    var freeAddress = App.Account
                        .GetFreeExternalAddressAsync(_currency.Name)
                        .WaitForResult();

                    FromAddressList = activeAddresses
                        .Select(wa => new WalletAddressViewModel(wa))
                        .ToList()
                        .AddEx(new WalletAddressViewModel(freeAddress, isFreeAddress: true));
#if DEBUG
                }
#endif
            }
        }

        private List<WalletAddressViewModel> _fromAddressList;
        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            private set
                {
                _fromAddressList = value;
                OnPropertyChanged(nameof(FromAddressList));

                SelectedAddress = GetDefaultAddress();
            }
        }

        private WalletAddress _selectedAddress;
        public WalletAddress SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                _selectedAddress = value;
                OnPropertyChanged(nameof(SelectedAddress));

                if (_selectedAddress != null)
                    CreateQrCodeAsync().FireAndForget();

                Warning = string.Empty;
            }
        }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        public BitmapSource QrCode { get; private set; }

        public ReceiveViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();         
#endif
        }

        public ReceiveViewModel(IAtomexApp app)
            : this(app, null)
        {
        }

        public ReceiveViewModel(IAtomexApp app, Currency currency)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            FromCurrencies = app.Account.Currencies
                .Where(c => c.IsTransactionsAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            Currency = currency;
        }

        private async Task CreateQrCodeAsync()
        {
            Bitmap qrCodeBitmap = null;

            await Task.Factory.StartNew(() =>
            {
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrData = qrGenerator.CreateQrCode(_selectedAddress.Address, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                    qrCodeBitmap = qrCode.GetGraphic(PixelsPerModule);
            });

            if (Application.Current.Dispatcher != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    QrCode = qrCodeBitmap.ToBitmapSource();
                    OnPropertyChanged(nameof(QrCode));

                    qrCodeBitmap.Dispose();
                });
            }
        }

        private WalletAddress GetDefaultAddress()
        {
            if (Currency is Tezos || Currency is Ethereum)
            {
                var activeAddressViewModel = FromAddressList
                    .FirstOrDefault(vm => vm.WalletAddress.HasActivity && vm.WalletAddress.AvailableBalance() > 0);

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.WalletAddress;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ?? (_copyCommand = new RelayCommand<string>((s) =>
        {
            try
            {
                Clipboard.SetText(SelectedAddress.Address);

                Warning = "Address successfully copied to clipboard.";
            }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        }));

        private void DesignerMode()
        {
            FromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            Currency = FromCurrencies.First().Currency;
           // Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM";
        }
    }
}