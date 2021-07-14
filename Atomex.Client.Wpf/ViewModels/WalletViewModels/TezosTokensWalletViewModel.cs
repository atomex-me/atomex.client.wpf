﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;

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
            ? $"{TokenBalance.Balance}  {TokenBalance.Symbol}"
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

        public static string RemoveIpfsPrefix(string url) => RemovePrefix(url, "ipfs://");

        public static bool HasIpfsPrefix(string url) => url?.StartsWith("ipfs://") ?? false;
    }

    public class TezosTokenContractViewModel : BaseViewModel
    {
        public TokenContract Contract { get; set; }
        public string IconUrl => $"https://services.tzkt.io/v1/avatars/{Contract.Address}";
    }

    public class TezosTokensWalletViewModel : BaseViewModel, IWalletViewModel
    {
        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }
        public ObservableCollection<TezosTokenViewModel> Tokens { get; set; }
        public ObservableCollection<TransactionViewModel> Transfers { get; set; }

        private TezosTokenContractViewModel _tokenContract;
        public TezosTokenContractViewModel TokenContract
        {
            get => _tokenContract;
            set
            {
                _tokenContract = value;
                OnPropertyChanged(nameof(TokenContract));

                TokenContractChanged(_tokenContract);
            }
        }

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

        private readonly IAtomexApp _app;

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
            _app = app ?? throw new ArgumentNullException(nameof(app));

            SubscribeToUpdates();

            _ = LoadAsync();

            DesignerMode();
        }

        private void SubscribeToUpdates()
        {
            _app.Account.BalanceUpdated += OnBalanceUpdatedEventHandler;
        }

        protected virtual async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                //if (Currency.Name == args.Currency)
                //{
                //    // update transactions list
                //    await LoadTransactionsAsync();
                //}
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task LoadAsync()
        {
            var tokensContractsViewModels = (await _app.Account
                .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz)
                .DataRepository
                .GetTezosTokenContractsAsync())
                .Select(c => new TezosTokenContractViewModel { Contract = c });

            TokensContracts = new ObservableCollection<TezosTokenContractViewModel>(tokensContractsViewModels);
            OnPropertyChanged(nameof(TokensContracts));
        }

        private void TokenContractChanged(TezosTokenContractViewModel tokenContractViewModel)
        {

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

            TokenContract = TokensContracts.First();

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