using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public class TezosTokenContractViewModel : BaseViewModel
    {
        public TokenContract Contract { get; set; }
        public string IconUrl => $"https://services.tzkt.io/v1/avatars/{Contract.Address}";
    }

    public class TezosTokensWalletViewModel : BaseViewModel, IWalletViewModel
    {
        public ObservableCollection<TezosTokenContractViewModel> TokensContracts { get; set; }

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
                }
            };
        }
    }
}