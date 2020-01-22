using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public class Delegation
    {
        public BakerData Baker { get; set; }
        public string Address { get; set; }
        public decimal Balance { get; set; }
    }

    public class TezosWalletViewModel : WalletViewModel
    {
        private const int DelegationCheckIntervalInSec = 20;

        private bool _canDelegate;
        public bool CanDelegate
        {
            get => _canDelegate;
            set { _canDelegate = value; OnPropertyChanged(nameof(CanDelegate)); }
        }

        private bool _hasDelegations;
        public bool HasDelegations
        {
            get => _hasDelegations;
            set { _hasDelegations = value; OnPropertyChanged(nameof(HasDelegations)); }
        }

        private List<Delegation> _delegations;
        public List<Delegation> Delegations
        {
            get => _delegations;
            set { _delegations = value; OnPropertyChanged(nameof(Delegations)); }
        }

        public TezosWalletViewModel()
            : base()
        {
        }

        public TezosWalletViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            IMenuSelector menuSelector,
            IConversionViewModel conversionViewModel,
            Currency currency)
            : base(app, dialogViewer, menuSelector, conversionViewModel, currency)
        {
            Delegations = new List<Delegation>();

            // update delegation info
            LoadDelegationInfoAsync().FireAndForget();
        }

        protected override async void OnBalanceUpdatedEventHandler(object sender, CurrencyEventArgs args)
        {
            try
            {
                if (Currency.Name == args.Currency.Name)
                {
                    // update transactions list
                    await LoadTransactionsAsync();

                    // update delegation info
                    await LoadDelegationInfoAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Account balance updated event handler error");
            }
        }

        private async Task LoadDelegationInfoAsync()
        {
            try
            {
                var tezos = Currency as Tezos;

                var balance = await App.Account
                    .GetBalanceAsync(tezos.Name)
                    .ConfigureAwait(false);

                var addresses = await App.Account
                    .GetUnspentAddressesAsync(tezos.Name)
                    .ConfigureAwait(false);

                var rpc = new Rpc(tezos.RpcNodeUri);

                var delegations = new List<Delegation>();

                foreach (var wa in addresses)
                {
                    var accountData = await rpc
                        .GetAccount(wa.Address)
                        .ConfigureAwait(false);

                    var @delegate = accountData["delegate"]?.ToString();

                    if (string.IsNullOrEmpty(@delegate))
                        continue;


                    var baker = await new BbApi(tezos)
                        .GetBaker(@delegate, App.Account.Network)
                        .ConfigureAwait(false);

                    delegations.Add(new Delegation
                    {
                        Baker = baker,
                        Address = wa.Address,
                        Balance = wa.Balance
                    });
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CanDelegate = balance.Available > 0;
                    Delegations = delegations;
                    HasDelegations = delegations.Count > 0;
                },
                DispatcherPriority.Background);
            }
            catch (Exception e)
            {
                Log.Error(e, "LoadDelegationInfoAsync error");
            }
        }

        private ICommand _delegateCommand;
        public ICommand DelegateCommand => _delegateCommand ?? (_delegateCommand = new Command(OnDelegateClick));

        //private ICommand _undelegateCommand;
        //public ICommand UndelegateCommand => _undelegateCommand ?? (_undelegateCommand = new Command(OnUndelegateClick));

        private void OnDelegateClick()
        {
            var viewModel = new DelegateViewModel(App, DialogViewer, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(DelegationCheckIntervalInSec))
                    .ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(OnUpdateClick);
            });

            DialogViewer.ShowDialog(Dialogs.Delegate, viewModel, defaultPageId: Pages.Delegate);
        }

        //private void OnUndelegateClick()
        //{
        //}

        protected override void DesignerMode()
        {
            base.DesignerMode();

            Delegations = new List<Delegation>()
            {
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                },
                new Delegation
                {
                    Baker = new BakerData {
                        Logo = "https://api.baking-bad.org/logos/letzbake.png"
                    },
                    Address = "tz1aqcYgG6NuViML5vdWhohHJBYxcDVLNUsE",
                    Balance = 1000.2123m
                }
            };

            HasDelegations = true;
        }
    }
}