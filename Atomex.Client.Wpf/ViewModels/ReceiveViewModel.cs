using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using QRCoder;
using Serilog;

using Atomex.Core;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ReceiveViewModel : BaseViewModel
    {
        private const int PixelsPerModule = 20;

        protected IAtomexApp App { get; }

        private List<CurrencyViewModel> _fromCurrencies;
        public List<CurrencyViewModel> FromCurrencies
        {
            get => _fromCurrencies;
            private set { _fromCurrencies = value; OnPropertyChanged(nameof(FromCurrencies)); }
        }

        protected CurrencyConfig _currency;
        public virtual CurrencyConfig Currency
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
                    // get all addresses with tokens (if exists)
                    var tokenAddresses = Currencies.HasTokens(_currency.Name)
                        ? App.Account
                            .GetCurrencyAccount<ILegacyCurrencyAccount>(_currency.Name)
                            .GetUnspentTokenAddressesAsync()
                            .WaitForResult()
                        : new List<WalletAddress>();

                    // get all active addresses
                    var activeAddresses = App.Account
                        .GetUnspentAddressesAsync(_currency.Name)
                        .WaitForResult()
                        .ToList();

                    // get free external address
                    var freeAddress = App.Account
                        .GetFreeExternalAddressAsync(_currency.Name)
                        .WaitForResult();

                    FromAddressList = activeAddresses
                        .Concat(tokenAddresses)
                        .Concat(new WalletAddress[] { freeAddress })
                        .GroupBy(w => w.Address)
                        .Select(g =>
                        {
                            var address      = g.FirstOrDefault(w => w.Currency == _currency.Name);
                            var hasTokens    = g.Any(w => w.Currency != _currency.Name);
                            var tokenBalance = TokenContract != null
                                ? (g.FirstOrDefault(w => w.TokenBalance?.Contract == TokenContract)?.Balance ?? 0)
                                : 0;
                            var isFreeAddress = address.Address == freeAddress.Address;

                            // if main chain address null => try to get address from db
                            if (address == null)
                            {
                                address = App.Account
                                    .GetAddressAsync(_currency.Name, g.Key)
                                    .WaitForResult();
                            }

                            // if main chain address null again => add address to db by token address key index
                            if (address == null)
                            {
                                var tokenAddress = g.First();

                                address = App.Account
                                    .GetCurrencyAccount<ILegacyCurrencyAccount>(_currency.Name)
                                    .DivideAddressAsync(tokenAddress.KeyIndex.Chain, tokenAddress.KeyIndex.Index)
                                    .WaitForResult();
                            }

                            return new WalletAddressViewModel(
                                walletAddress: address,
                                currencyFormat: _currency.Format,
                                hasTokens: hasTokens,
                                tokensBalance: tokenBalance,
                                isFreeAddress: isFreeAddress);
                        })
                        .ToList();
#if DEBUG
                }
#endif
            }
        }

        private List<WalletAddressViewModel> _fromAddressList;
        public List<WalletAddressViewModel> FromAddressList
        {
            get => _fromAddressList;
            protected set
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
                   _ =  CreateQrCodeAsync();

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

        public string TokenContract { get; private set; }
        public bool ShowTokenBalance => TokenContract != null;

        public ReceiveViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();         
#endif
        }

        public ReceiveViewModel(IAtomexApp app, CurrencyConfig currency, string tokenContract = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));

            FromCurrencies = app.Account.Currencies
                .Select(CurrencyViewModelCreator.CreateViewModel)
                .ToList();

            Currency = FromCurrencies
                .FirstOrDefault(c => c.Currency.Name == currency.Name)
                .Currency;

            TokenContract = tokenContract;
        }

        private async Task CreateQrCodeAsync()
        {
            Bitmap qrCodeBitmap = null;

            await Task.Run(() =>
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrData = qrGenerator.CreateQrCode(_selectedAddress.Address, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
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

        protected virtual WalletAddress GetDefaultAddress()
        {
            if (Currency is TezosConfig || Currency is EthereumConfig)
            {
                var activeAddressViewModel = FromAddressList
                    .Where(vm => vm.WalletAddress.HasActivity && vm.WalletAddress.AvailableBalance() > 0)
                    .MaxByOrDefault(vm => vm.WalletAddress.AvailableBalance());

                if (activeAddressViewModel != null)
                    return activeAddressViewModel.WalletAddress;
            }

            return FromAddressList.First(vm => vm.IsFreeAddress).WalletAddress;
        }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new RelayCommand<string>((s) =>
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
        });

        private void DesignerMode()
        {
            FromCurrencies = DesignTime.Currencies
                .Select(c => CurrencyViewModelCreator.CreateViewModel(c, subscribeToUpdates: false))
                .ToList();

            Currency = FromCurrencies
                .First()
                .Currency;

            FromAddressList = new List<WalletAddressViewModel>
            {
                new WalletAddressViewModel(
                    walletAddress: new WalletAddress
                    {
                        Address     = "tz3bvNMQ95vfAYtG8193ymshqjSvmxiCUuR5",
                        Currency    = Currency.Name,
                        Balance     = 123.456789m,
                        HasActivity = true
                    },
                    currencyFormat: Currency.Format,
                    hasTokens: true,
                    tokensBalance: 100.000000m,
                    isFreeAddress: false),
                new WalletAddressViewModel(
                    walletAddress: new WalletAddress
                    {
                        Address     = "tz3bvNMQ95vfAYtG8193ymshqjSvmxiCUuR5",
                        Currency    = Currency.Name,
                        Balance     = 0.00000m,
                        HasActivity = false
                    },
                    currencyFormat: Currency.Format,
                    hasTokens: false,
                    tokensBalance: 0,
                    isFreeAddress: true)
            };
        }
    }
}