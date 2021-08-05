using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows;

using Serilog;
using MahApps.Metro.Controls.Dialogs;

using Atomex.Core;
using Atomex.Common;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.TezosTokens;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Wpf.ViewModels
{
    public class AddressInfo
    {
        public string Address { get; set; }
        public string Path { get; set; }
        public string Balance { get; set; }
        public string TokenBalance { get; set; }
        public Action<string> CopyToClipboard { get; set; }
        public Action<string> OpenInExplorer { get; set; }
        public Action<string> Update { get; set; }
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

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= new RelayCommand<string>((s) =>
        {
            Update?.Invoke(Address);
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
        private CurrencyConfig _currency;
        private bool _isBalanceUpdating;
        private CancellationTokenSource _cancellation;
        private string _tokenContract;

        public IEnumerable<AddressInfo> Addresses { get; set; }
        public bool HasTokens { get; set; }

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

        public AddressesViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            CurrencyConfig currency,
            string tokenContract = null)
        {
            _app           = app ?? throw new ArgumentNullException(nameof(app));
            _dialogViewer  = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            _currency      = currency ?? throw new ArgumentNullException(nameof(currency));
            _tokenContract = tokenContract;

            RealodAddresses();
        }

        public async void RealodAddresses()
        {
            try
            {
                var account = _app.Account
                    .GetCurrencyAccount(_currency.Name);

                var addresses = (_currency switch
                {
                    Fa12Config fa12Config => await (account as Fa12Account).DataRepository
                        .GetTezosTokenAddressesByContractAsync(fa12Config.TokenContractAddress),

                    _ => await account
                        .GetAddressesAsync()

                }).ToList();

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
                    Update          = Update,
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

        private async void Update(string address)
        {
            if (_isBalanceUpdating)
                return;

            _isBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();

            await _dialogViewer.ShowProgressAsync(
                title: "Address balance updating...",
                message: "Please wait!",
                canceled: () => { _cancellation.Cancel(); });

            try
            {
                await new HdWalletScanner(_app.Account)
                    .ScanAddressAsync(_currency.Name, address, _cancellation.Token);

                if (_currency.Name == TezosConfig.Xtz && _tokenContract != null)
                {
                    // update tezos token balance
                    var tezosAccount = _app.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    await new TezosTokensScanner(tezosAccount)
                        .ScanContractAsync(address, _tokenContract);

                    // reload balances for all tezos tokens account
                    foreach (var currency in _app.Account.Currencies)
                        if (Currencies.IsTezosToken(currency.Name))
                            _app.Account
                                .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                                .ReloadBalances();
                }

                RealodAddresses();

            }
            catch (OperationCanceledException)
            {
                Log.Debug("Address balance update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "AddressesViewModel.OnUpdateClick");
                // todo: message to user!?
            }

            _dialogViewer.HideProgress();

            _isBalanceUpdating = false;
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
                    Path    = "m/44'/0'/0'/0/0",
                    Balance = 4.0000000.ToString(CultureInfo.InvariantCulture),
                },
                new AddressInfo
                {
                    Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                    Path    = "m/44'/0'/0'/0/0",
                    Balance = 100.ToString(CultureInfo.InvariantCulture),
                },
                new AddressInfo
                {
                    Address = "mzztP8VVJYxV93EUiiYrJUbL55MLx7KLoM",
                    Path    = "m/44'/0'/0'/0/0",
                    Balance = 16.0000001.ToString(CultureInfo.InvariantCulture),
                }
            };
        }
    }
}