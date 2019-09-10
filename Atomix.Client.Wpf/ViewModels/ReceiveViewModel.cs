using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Atomix.Core.Entities;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Common;
using Helpers;
using QRCoder;

namespace Atomix.Client.Wpf.ViewModels
{
    public class ReceiveViewModel : BaseViewModel
    {
        private const int PixelsPerModule = 20;

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
#endif
                    Address = App.AtomixApp.Account
                        .GetFreeExternalAddressAsync(_currency)
                        .WaitForResult()
                        .Address;
            }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));

                CreateQrCodeAsync().FireAndForget();
            }
        }

        public BitmapSource QrCode { get; private set; }

        public ReceiveViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();         
#endif
        }

        public ReceiveViewModel(IAtomixApp app)
        {
            FromCurrencies = app.Account.Currencies
                .Where(c => c.IsTransactionsAvailable)
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();
        }

        private async Task CreateQrCodeAsync()
        {
            Bitmap qrCodeBitmap = null;

            await Task.Factory.StartNew(() =>
            {
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrData = qrGenerator.CreateQrCode(Address, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                    qrCodeBitmap = qrCode.GetGraphic(PixelsPerModule);
            });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                QrCode = qrCodeBitmap.ToBitmapSource();
                OnPropertyChanged(nameof(QrCode));

                qrCodeBitmap.Dispose();
            });
        }

        private void DesignerMode()
        {
            FromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            Currency = FromCurrencies.First().Currency;
            Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM";
        }
    }
}