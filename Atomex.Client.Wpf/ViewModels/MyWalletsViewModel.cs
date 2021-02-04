using System;
using System.Collections.Generic;
using System.Windows.Input;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Common;
using Atomex.Core;
using Atomex.Subsystems;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class MyWalletsViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; }
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
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            Wallets = WalletInfo.AvailableWallets();
        }

        private ICommand _selectWalletCommand;
        public ICommand SelectWalletCommand => _selectWalletCommand ??= new RelayCommand<WalletInfo>(info =>
        {
            IAccount account = null;

            var unlockViewModel = new UnlockViewModel(info.Name, password =>
            {
                account = Account.LoadFromFile(
                    pathToAccount: info.Path,
                    password: password,
                    currenciesProvider: AtomexApp.CurrenciesProvider,
                    clientType: ClientType.Wpf);
            });

            unlockViewModel.Unlocked += (sender, args) =>
            {
                var atomexClient = new WebSocketAtomexClient(
                    configuration: App.Configuration,
                    account: account,
                    symbolsProvider: AtomexApp.SymbolsProvider,
                    quotesProvider: AtomexApp.QuotesProvider);

                AtomexApp.UseTerminal(atomexClient, restart: true);

                DialogViewer.HideDialog(Dialogs.MyWallets);
                DialogViewer.HideDialog(Dialogs.Start);
                DialogViewer.HideDialog(Dialogs.Unlock);
            };

            DialogViewer.ShowDialog(Dialogs.Unlock, unlockViewModel);
        });

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