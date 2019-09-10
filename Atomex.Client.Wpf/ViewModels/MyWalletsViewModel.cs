﻿using System;
using System.Collections.Generic;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class MyWalletsViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        public IEnumerable<WalletInfo> Wallets { get; set; }
        // public bool HasAvailableWallets => Wallets != null && Wallets.Any();

        public MyWalletsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public MyWalletsViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            Wallets = WalletInfo.AvailableWallets();
        }

        private ICommand _selectWalletCommand;
        public ICommand SelectWalletCommand => _selectWalletCommand ?? (_selectWalletCommand = new RelayCommand<WalletInfo>(info =>
        {
            IAccount account = null;

            var unlockViewModel = new UnlockViewModel(info.Name, password =>
            {
                account = Account.LoadFromFile(
                    pathToAccount: info.Path,
                    password: password,
                    currenciesProvider: App.CurrenciesProvider,
                    symbolsProvider: App.SymbolsProvider);
            });

            unlockViewModel.Unlocked += (sender, args) =>
            {
                App.UseAccount(account, restartTerminal: true);

                DialogViewer?.HideMyWalletsDialog();
                DialogViewer?.HideStartDialog();
                DialogViewer?.HideUnlockDialog();
            };

            DialogViewer?.ShowUnlockDialog(unlockViewModel);
        }));


        private void DesignerMode()
        {
            Wallets = new List<WalletInfo>
            {
                new WalletInfo {Name = "default", Path = "wallets/default/", Network = Network.MainNet},
                new WalletInfo {Name = "market_maker", Path = "wallets/marketmaker/", Network = Network.MainNet},
                new WalletInfo {Name = "wallet1", Path = "wallets/default/", Network = Network.TestNet},
                new WalletInfo {Name = "my_first_wallet", Path = "wallets/marketmaker/", Network = Network.TestNet},
                new WalletInfo {Name = "mega_wallet", Path = "wallets/marketmaker/", Network = Network.MainNet}
            };
        }
    }
}