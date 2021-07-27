using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Threading;

using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Client.Wpf.ViewModels.TransactionViewModels;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public class TezosTokenViewModel : BaseViewModel
    {
        private bool _isPreviewDownloading = false;

        public TokenBalance TokenBalance { get; set; }

        public BitmapImage TokenPreview
        {
            get
            {
                if (_isPreviewDownloading)
                    return null;

                foreach (var url in GetTokenPreviewUrls())
                {
                    var previewBytesFromCache = MemoryCache.Default.Get(url) as byte[];

                    if (previewBytesFromCache == null)
                    {
                        // start async download
                        _ = Task.Run(async () =>
                        {
                            await FromUrlAsync(url)
                                .ConfigureAwait(false);
                        });

                        return null;
                    }

                    // skip url without content
                    if (!previewBytesFromCache.Any())
                        continue;

                    using var previewStream = new MemoryStream(previewBytesFromCache);

                    var preview = new BitmapImage();

                    preview.BeginInit();
                    preview.CacheOption = BitmapCacheOption.OnLoad;
                    preview.StreamSource = previewStream;
                    preview.EndInit();

                    return preview;
                }


                return null;
            }
        }

        public string Balance => TokenBalance.Balance != "1"
            ? $"{TokenBalance.GetTokenBalance().ToString(CultureInfo.InvariantCulture)}  {TokenBalance.Symbol}"
            : "";

        private ICommand _openInBrowser;
        public ICommand OpenInBrowser => _openInBrowser ??= new Command(() =>
        {
            var assetUrl = AssetUrl;

            if (assetUrl != null && Uri.TryCreate(assetUrl, UriKind.Absolute, out var uri))
                Process.Start(uri.ToString());
            else
                Log.Error("Invalid uri for ipfs asset");
        });

        public bool IsIpfsAsset => TokenBalance.ArtifactUri != null && HasIpfsPrefix(TokenBalance.ArtifactUri);

        public string AssetUrl => IsIpfsAsset
            ? $"http://ipfs.io/ipfs/{RemoveIpfsPrefix(TokenBalance.ArtifactUri)}"
            : null;

        private async Task FromUrlAsync(string url)
        {
            _isPreviewDownloading = true;

            try
            {
                var response = await HttpHelper.HttpClient
                    .GetAsync(url)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var previewBytes = await response.Content
                        .ReadAsByteArrayAsync()
                        .ConfigureAwait(false);

                    MemoryCache.Default.Set(url, previewBytes, DateTimeOffset.MaxValue);
                }
                else if (response.StatusCode == HttpStatusCode.BadGateway ||
                         response.StatusCode == HttpStatusCode.GatewayTimeout ||
                         response.StatusCode == HttpStatusCode.InternalServerError ||
                         response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    MemoryCache.Default.Set(url, new byte[0], DateTimeOffset.Now.AddMinutes(10));
                }
                else
                {
                    MemoryCache.Default.Set(url, new byte[0], DateTimeOffset.MaxValue);
                }
            }
            catch
            {
                MemoryCache.Default.Set(url, new byte[0], DateTimeOffset.Now.AddMinutes(10));
            }

            _isPreviewDownloading = false;

            await Application
                .Current
                .Dispatcher
                .InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(TokenPreview));
                });
        }

        public IEnumerable<string> GetTokenPreviewUrls()
        {
            yield return $"https://d38roug276qjor.cloudfront.net/{TokenBalance.Contract}/{TokenBalance.TokenId}.png";

            if (TokenBalance.ArtifactUri != null && HasIpfsPrefix(TokenBalance.ArtifactUri))
                yield return $"https://api.dipdup.net/thumbnail/{RemoveIpfsPrefix(TokenBalance.ArtifactUri)}";

            yield return $"https://services.tzkt.io/v1/avatars/{TokenBalance.Contract}";
        }

        public static string RemovePrefix(string s, string prefix) =>
            s.StartsWith(prefix) ? s.Substring(prefix.Length) : s;

        public static string RemoveIpfsPrefix(string url) =>
            RemovePrefix(url, "ipfs://");

        public static bool HasIpfsPrefix(string url) =>
            url?.StartsWith("ipfs://") ?? false;
    }

    public class TezosTokenContractViewModel : BaseViewModel
    {
        public TokenContract Contract { get; set; }
        public string IconUrl => $"https://services.tzkt.io/v1/avatars/{Contract.Address}";
        public bool IsFa12 => Contract.GetContractType() == "FA12";
        public bool IsFa2 => Contract.GetContractType() == "FA2";
    }

    public class TezosTokensWalletViewModel : BaseViewModel, IWalletViewModel
    {
        private const int MaxAmountDecimals = 9;
        private const string Fa12 = "FA12";

        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TezosTokenTransferViewModel> Transfers { get; set; }

        private TezosTokenContractViewModel _tokenContract;
        public TezosTokenContractViewModel TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));
                OnPropertyChanged(nameof(HasTokenContract));
                OnPropertyChanged(nameof(IsFa12));
                OnPropertyChanged(nameof(IsFa2));
                OnPropertyChanged(nameof(TokenContractAddress));
                OnPropertyChanged(nameof(TokenContractName));
                OnPropertyChanged(nameof(TokenContractIconUrl));

                TokenContractChanged(TokenContract);
            }
        }

        public bool HasTokenContract => TokenContract != null;
        public bool IsFa12 => TokenContract?.IsFa12 ?? false;
        public bool IsFa2 => TokenContract?.IsFa2 ?? false;
        public string TokenContractAddress => TokenContract?.Contract?.Address ?? "";
        public string TokenContractName => TokenContract?.Contract?.Name ?? "";
        public string TokenContractIconUrl => TokenContract?.IconUrl;

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

        public decimal Balance { get; set; }
        public string BalanceFormat { get; set; }
        public string BalanceCurrencyCode { get; set; }

        public Brush Background => IsSelected
            ? TezosCurrencyViewModel.DefaultIconBrush
            : TezosCurrencyViewModel.DefaultUnselectedIconBrush;

        public Brush OpacityMask => IsSelected
            ? null
            : TezosCurrencyViewModel.DefaultIconMaskBrush;

        public int SelectedTabIndex { get; set; }

        private readonly IAtomexApp _app;
        private readonly IDialogViewer _dialogViewer;
        private readonly IMenuSelector _menuSelector;
        private readonly IConversionViewModel _conversionViewModel;
        private bool _isBalanceUpdating;
        private CancellationTokenSource _cancellation;

        public TezosTokensWalletViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public TezosTokensWalletViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel)
        {
            _app                 = app ?? throw new ArgumentNullException(nameof(app));
            _dialogViewer        = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            _menuSelector        = menuSelector ?? throw new ArgumentNullException(nameof(menuSelector));
            _conversionViewModel = conversionViewModel ?? throw new ArgumentNullException(nameof(conversionViewModel));

            SubscribeToUpdates();

            _ = ReloadTokenContractsAsync();
        }

        private void SubscribeToUpdates()
        {
            _app.AtomexClientChanged    += OnAtomexClientChanged;
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        private void OnAtomexClientChanged(object sender, Services.AtomexClientChangedEventArgs e)
        {
            Tokens?.Clear();
            Transfers?.Clear();
            TokensContracts?.Clear();
            TokenContract = null;
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currencies.IsTezosToken(args.Currency))
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await ReloadTokenContractsAsync();

                    }, DispatcherPriority.Background);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task ReloadTokenContractsAsync()
        {
            var tokensContractsViewModels = (await _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                .DataRepository
                .GetTezosTokenContractsAsync())
                .Select(c => new TezosTokenContractViewModel { Contract = c });

            if (TokensContracts != null)
            {
                // add new token contracts if exists
                var newTokenContracts = tokensContractsViewModels.Except(
                    second: TokensContracts,
                    comparer: new Atomex.Common.EqualityComparer<TezosTokenContractViewModel>(
                        (x, y) => x.Contract.Address.Equals(y.Contract.Address),
                        x => x.Contract.Address.GetHashCode()));

                if (newTokenContracts.Any())
                {
                    foreach (var newTokenContract in newTokenContracts)
                        TokensContracts.Add(newTokenContract);

                    if (TokenContract == null)
                        TokenContract = TokensContracts.FirstOrDefault();
                }
                else
                {
                    // update current token contract
                    if (TokenContract != null)
                        TokenContractChanged(TokenContract);
                }
            }
            else
            {
                TokensContracts = new ObservableCollection<TezosTokenContractViewModel>(tokensContractsViewModels);
                OnPropertyChanged(nameof(TokensContracts));

                TokenContract = TokensContracts.FirstOrDefault();
            }
        }

        private async void TokenContractChanged(TezosTokenContractViewModel tokenContract)
        {
            if (tokenContract == null)
            {
                Tokens    = new ObservableCollection<TezosTokenViewModel>();
                Transfers = new ObservableCollection<TezosTokenTransferViewModel>();

                OnPropertyChanged(nameof(Tokens));
                OnPropertyChanged(nameof(Transfers));

                return;
            }

            var tezosConfig = _app.Account
                .Currencies
                .Get<TezosConfig>(TezosConfig.Xtz);

            if (tokenContract.IsFa12)
            {
                var tokenAccount = _app.Account.GetTezosTokenAccount<Fa12Account>(
                    currency: Fa12,
                    tokenContract: tokenContract.Contract.Address,
                    tokenId: 0);

                var tokenAddresses = await tokenAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                var tokenAddress = tokenAddresses.FirstOrDefault();

                Balance = tokenAccount
                    .GetBalance()
                    .Available;

                BalanceFormat = tokenAddress?.TokenBalance != null
                    ? $"F{Math.Min(tokenAddress.TokenBalance.Decimals, MaxAmountDecimals)}"
                    : $"F{MaxAmountDecimals}";

                BalanceCurrencyCode = tokenAddress?.TokenBalance != null
                    ? tokenAddress.TokenBalance.Symbol
                    : "";

                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(BalanceFormat));
                OnPropertyChanged(nameof(BalanceCurrencyCode));

                Transfers = new ObservableCollection<TezosTokenTransferViewModel>((await tokenAccount
                    .DataRepository
                    .GetTezosTokenTransfersAsync(tokenContract.Contract.Address))
                    .Select(t => new TezosTokenTransferViewModel(t, tezosConfig)));

                Tokens = new ObservableCollection<TezosTokenViewModel>();
            }
            else if (tokenContract.IsFa2)
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tokenAddresses = await tezosAccount
                    .DataRepository
                    .GetTezosTokenAddressesByContractAsync(tokenContract.Contract.Address);

                Transfers = new ObservableCollection<TezosTokenTransferViewModel>((await tezosAccount
                    .DataRepository
                    .GetTezosTokenTransfersAsync(tokenContract.Contract.Address))
                    .Select(t => new TezosTokenTransferViewModel(t, tezosConfig)));

                Tokens = new ObservableCollection<TezosTokenViewModel>(tokenAddresses
                    .Select(a => new TezosTokenViewModel { TokenBalance = a.TokenBalance }));
            }

            OnPropertyChanged(nameof(Tokens));
            OnPropertyChanged(nameof(Transfers));

            SelectedTabIndex = tokenContract.IsFa2 ? 0 : 1;
            OnPropertyChanged(nameof(SelectedTabIndex));
        }

        private ICommand _receiveCommand;
        public ICommand ReceiveCommand => _receiveCommand ??= new Command(OnReceiveClick);

        private ICommand _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= new Command(OnUpdateClick);

        private void OnReceiveClick()
        {
            var tezosConfig = _app.Account.Currencies.GetByName(TezosConfig.Xtz);
            var receiveViewModel = new ReceiveViewModel(_app, tezosConfig, TokenContract?.Contract?.Address);

            _dialogViewer.ShowDialog(Dialogs.Receive, receiveViewModel);
        }

        protected async void OnUpdateClick()
        {
            if (_isBalanceUpdating)
                return;

            _isBalanceUpdating = true;

            _cancellation = new CancellationTokenSource();

            _dialogViewer.ShowProgress(
                title: "Tokens balance updating...",
                message: "Please wait!",
                canceled: () => { _cancellation.Cancel(); });

            try
            {
                var tezosAccount = _app.Account
                    .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                var tezosTokensScanner = new TezosTokensScanner(tezosAccount);

                await tezosTokensScanner.ScanAsync(
                    skipUsed: false,
                    cancellationToken: _cancellation.Token);

                // reload balances for all tezos tokens account
                foreach (var currency in _app.Account.Currencies)
                    if (Currencies.IsTezosToken(currency.Name))
                        _app.Account
                            .GetCurrencyAccount<TezosTokenAccount>(currency.Name)
                            .ReloadBalances();

            }
            catch (OperationCanceledException)
            {
                Log.Debug("Wallet update operation canceled");
            }
            catch (Exception e)
            {
                Log.Error(e, "WalletViewModel.OnUpdateClick");
                // todo: message to user!?
            }

            _dialogViewer.HideProgress();

            _isBalanceUpdating = false;
        }

        protected void DesignerMode()
        {
            TokensContracts = new ObservableCollection<TezosTokenContractViewModel>
            {
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1K9gCRgaLRFKTErYt1wVxA3Frb9FjasjTV",
                        Network     = "mainnet",
                        Name        = "kUSD",
                        Description = "FA1.2 Implementation of kUSD",
                        Interfaces  = new List<string> { "TZIP-007-2021-01-29" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1PWx2mnDueood7fEmfbBDKx1D9BAnnXitn",
                        Network     = "mainnet",
                        Name        = "tzBTC",
                        Description = "Wrapped Bitcon",
                        Interfaces  = new List<string> { "TZIP-7", "TZIP-16", "TZIP-20" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1G1cCRNBgQ48mVDjopHjEmTN5Sbtar8nn9",
                        Network     = "mainnet",
                        Name        = "Hedgehoge",
                        Description = "such cute, much hedge!",
                        Interfaces  = new List<string> { "TZIP-007", "TZIP-016" }
                    }
                },
                new TezosTokenContractViewModel
                {
                    Contract = new TokenContract
                    {
                        Address     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
                        Network     = "mainent",
                        Name        = "hic et nunc NFTs",
                        Description = "NFT token for digital assets.",
                        Interfaces  = new List<string> { "TZIP-12" }
                    }
                }
            };

            _tokenContract = null;// TokensContracts.First();

            var bcdApi = new BcdApi(new BcdApiSettings
            {
                MaxSize = 10,
                Network = "mainnet",
                Uri     = "https://api.better-call.dev/v1/"
            });

            var tokensBalances = bcdApi
                .GetTokenBalancesAsync(
                    address: "tz1YS2CmS5o24bDz9XNr84DSczBXuq4oGHxr",
                    count: 36)
                .WaitForResult();

            Tokens = new ObservableCollection<TezosTokenViewModel>(
                tokensBalances.Value.Select(tb => new TezosTokenViewModel { TokenBalance = tb }));

            //Tokens = new ObservableCollection<TezosTokenViewModel>
            //{
            //    new TezosTokenViewModel
            //    {
            //        TokenBalance = new TokenBalance
            //        {
            //            Contract     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //            TokenId      = 155458,
            //            Symbol       = "OBJKT",
            //            Name         = "Enter VR Mode",
            //            Description  = "VR Mode Collection 1/6",
            //            ArtifactUri  = "ipfs://QmcxKgcESGphkb6S9k2Mh8jto6kapKtYN52mH1dBSFT6X5",
            //            DisplayUri   = "ipfs://QmQRqbdfz8xGzobjcczGmz31cMHcs2okMw2oFgpjtvggoF",
            //            ThumbnailUri = "ipfs://QmNrhZHUaEqxhyLfqoq1mtHSipkWHeT31LNHb1QEbDHgnc",
            //            Balance      = "1"
            //        }
            //    },
            //    new TezosTokenViewModel
            //    {
            //        TokenBalance = new TokenBalance
            //        {
            //            Contract     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //            TokenId      = 155265,
            //            Symbol       = "OBJKT",
            //            Name         = "Rooted.",
            //            Description  = "A high hillside with a rooted main character.",
            //            ArtifactUri  = "ipfs://QmapL8cQqVfVfmKNMbzH82kTZVW28qMoagoFCgDXRZmKgU",
            //            DisplayUri   = "ipfs://QmeTWEdhg9gDCavV8tS25fyBfYtXftzjcAFMcBbxaYyyLH",
            //            ThumbnailUri = "ipfs://QmNrhZHUaEqxhyLfqoq1mtHSipkWHeT31LNHb1QEbDHgnc",
            //            Balance      = "1000"
            //        }
            //    },
            //    new TezosTokenViewModel
            //    {
            //        TokenBalance = new TokenBalance
            //        {
            //            Contract     = "KT1RJ6PbjHpwc3M5rw5s2Nbmefwbuwbdxton",
            //            TokenId      = 154986,
            //            Symbol       = "OBJKT",
            //            Name         = "⭕️ MLNDR Founders Collection: 003/Greg",
            //            Description  = "Meet 12 year old Greg, the family’s firstborn. During his short life, he has gone through so much, yet he stays positive and acts as a pillar to keep his parents and little sister happy and motivated.  Set in a dystopian future where natural resources are scarce and pollution is a global problem, the MLNDR Family Series will portray how I see our future as a species if our hunger for non-renewable natural resources continues to grow at the current pace.   The Founders collection will be comprised of 6 characters who will play as leading actors in my MLNDR Family series.   Holders of this token participate for a chance to win one of 10 NFTs from my props collection.",
            //            ArtifactUri  = "ipfs://Qmd6rNTbeviB4tGruY27z47wAs27yRGHTDyp7b2qhxLHtU",
            //            DisplayUri   = "ipfs://QmaJXJJBpfyMMXA5RvU1du4zGfvytt7VJLHvgCNeHzWEWA",
            //            ThumbnailUri = "ipfs://QmNrhZHUaEqxhyLfqoq1mtHSipkWHeT31LNHb1QEbDHgnc",
            //            Balance      = "1"
            //        }
            //    }
            //};
        }
    }
}