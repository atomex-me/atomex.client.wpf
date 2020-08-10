using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Windows.Input;
using Serilog;

using Atomex.Core;
using Atomex.Client.Wpf.Common;
using System.Windows;
using System.Diagnostics;
using Atomex.Client.Wpf.Controls;
using MahApps.Metro.Controls.Dialogs;
using Atomex.Wallet;
using Atomex.Common;

namespace Atomex.Client.Wpf.ViewModels
{
    public class AddressInfo
    {
        public string Address { get; set; }
        public string Path { get; set; }
        public string Balance { get; set; }
        public Action<string> CopyToClipboard { get; set; }
        public Action<string> OpenInExplorer { get; set; }
        public Action<string> ExportKey { get; set; }

        private ICommand _copyCommand;
        public ICommand CopyCommand => _copyCommand ??= new RelayCommand<string>((s) =>
        {
            CopyToClipboard?.Invoke(Address);
        });

        private ICommand _openInExplorerCommand;
        public ICommand OpenInExplorerCommand => _openInExplorerCommand ??= new RelayCommand<string>((s) =>
        {
            OpenInExplorer?.Invoke(Address);
        });

        private ICommand _exportKeyCommand;
        public ICommand ExportKeyCommand => _exportKeyCommand ??= new RelayCommand<string>((s) =>
        {
            ExportKey?.Invoke(Address);
        });
    }

    public class AddressesViewModel : BaseViewModel
    {
        private readonly IAtomexApp _app;
        private readonly IDialogViewer _dialogViewer;
        private Currency _currency;

        public IEnumerable<AddressInfo> Addresses { get; set; }

        private string _warning;
        public string Warning {
            get => _warning;
            set
            {
                _warning = value;
                OnPropertyChanged(nameof(Warning));
                OnPropertyChanged(nameof(HasWarning));
            }
        }
        public bool HasWarning => !string.IsNullOrEmpty(Warning);

        public AddressesViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public AddressesViewModel(IAtomexApp app, IDialogViewer dialogViewer, Currency currency)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _dialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));

            Load();
        }

        public async void Load()
        {
            try
            {
                var account = _app.Account.GetCurrencyAccount(_currency.Name);

                var addresses = (await account.GetAddressesAsync())
                    .ToList();

                addresses.Sort((a1, a2) =>
                {
                    var chainResult = a1.KeyIndex.Chain.CompareTo(a2.KeyIndex.Chain);

                    return chainResult == 0
                        ? a1.KeyIndex.Index.CompareTo(a2.KeyIndex.Index)
                        : chainResult;
                });

                Addresses = addresses.Select(a => new AddressInfo
                {
                    Address         = a.Address,
                    Path            = $"m/44'/{_currency.Bip44Code}/0'/{a.KeyIndex.Chain}/{a.KeyIndex.Index}",
                    Balance         = a.Balance.ToString(CultureInfo.InvariantCulture),
                    CopyToClipboard = CopyToClipboard,
                    OpenInExplorer  = OpenInExplorer,
                    ExportKey       = ExportKey
                });

                OnPropertyChanged(nameof(Addresses));
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while load addresses.");
            }
        }

        private void CopyToClipboard(string address)
        {
            try
            {
                Clipboard.SetText(address);

                Warning = "Address successfully copied to clipboard.";
             }
            catch (Exception e)
            {
                Log.Error(e, "Copy to clipboard error");
            }
        }

        private void OpenInExplorer(string address)
        {
            try
            {
                if (Uri.TryCreate($"{_currency.AddressExplorerUri}{address}", UriKind.Absolute, out var uri))
                    Process.Start(uri.ToString());
                else
                    Log.Error("Invalid uri for address explorer");
            }
            catch (Exception e)
            {
                Log.Error(e, "Open in explorer error");
            }
        }

        private async void ExportKey(string address)
        {
            try
            {
                var result = await _dialogViewer.ShowMessageAsync(
                    "Warning!",
                    "Copying the private key to the clipboard may result in the loss of all your coins in the wallet. Are you sure you want to copy the private key?",
                    MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Affirmative)
                {
                    var walletAddress = await _app.Account
                        .GetAddressAsync(_currency.Name, address);

                    var hdWallet = _app.Account.Wallet as HdWallet;

                    using var privateKey = hdWallet.KeyStorage
                        .GetPrivateKey(_currency, walletAddress.KeyIndex);

                    using var unsecuredPrivateKey = privateKey.ToUnsecuredBytes();

                    var hex = Hex.ToHexString(unsecuredPrivateKey.Data);

                    Clipboard.SetText(hex);

                    Warning = "Private key successfully copied to clipboard.";
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Private key export error");
            }
        }

        private void DesignerMode()
        {
            _currency = DesignTime.Currencies.First();

            Addresses = new List<AddressInfo>
            {
                new AddressInfo
                {
                    Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                    Path = "m/44'/0'/0'/0/0",
                    Balance = 4.0000000.ToString(CultureInfo.InvariantCulture),
                },
                new AddressInfo
                {
                    Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                    Path = "m/44'/0'/0'/0/0",
                    Balance = 100.ToString(CultureInfo.InvariantCulture),
                },
                new AddressInfo
                {
                    Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                    Path = "m/44'/0'/0'/0/0",
                    Balance = 16.0000001.ToString(CultureInfo.InvariantCulture),
                }
            };
        }
    }
}