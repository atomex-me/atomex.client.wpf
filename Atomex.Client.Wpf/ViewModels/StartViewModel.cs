using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Subsystems;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        private IAtomexApp AtomexApp { get; }
        private IDialogViewer DialogViewer { get; }

        private bool _hasWallets;
        public bool HasWallets {
            get => _hasWallets;
            private set { _hasWallets = value; OnPropertyChanged(nameof(HasWallets)); }
        }

        public StartViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public StartViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            HasWallets = WalletInfo.AvailableWallets().Count() > 0;
        }

        private ICommand _myWalletsCommand;
        public ICommand MyWalletsCommand => _myWalletsCommand ??= new Command(() =>
        {
            DialogViewer.ShowDialog(Dialogs.MyWallets, new MyWalletsViewModel(AtomexApp, DialogViewer));
        });

        private ICommand _createNewCommand;
        public ICommand CreateNewCommand => _createNewCommand ??= new Command(() =>
        {
            DialogViewer.ShowDialog(Dialogs.CreateWallet,
                new CreateWalletViewModel(
                    app: AtomexApp,
                    scenario: CreateWalletScenario.CreateNew,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
        });

        private ICommand _restoreByMnemonicCommand;
        public ICommand RestoreByMnemonicCommand => _restoreByMnemonicCommand ??= new Command(() =>
        {
            DialogViewer.ShowDialog(Dialogs.CreateWallet,
                new CreateWalletViewModel(
                    app: AtomexApp,
                    scenario: CreateWalletScenario.Restore,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
        });

        private ICommand _twitterCommand;
        public ICommand TwitterCommand => _twitterCommand ??= new Command(() =>
        {
            Process.Start("https://twitter.com/atomex_official");
        });

        private ICommand _telegramCommand;
        public ICommand TelegramCommand => _telegramCommand ??= new Command(() =>
        {
            Process.Start("tg://resolve?domain=atomex_official");
        });

        private ICommand _githubCommand;
        public ICommand GithubCommand => _githubCommand ??= new Command(() =>
        {
            Process.Start("https://github.com/atomex-me");
        });

        private void OnCanceled()
        {
            DialogViewer.HideDialog(Dialogs.CreateWallet);
        }

        private void OnAccountCreated(IAccount account)
        {
            var atomexClient = new WebSocketAtomexClient(
                configuration: App.Configuration,
                account: account,
                symbolsProvider: AtomexApp.SymbolsProvider,
                quotesProvider: AtomexApp.QuotesProvider);

            AtomexApp.UseTerminal(atomexClient, restart: true);

            DialogViewer?.HideDialog(Dialogs.CreateWallet);
            DialogViewer?.HideDialog(Dialogs.Start);
        }

        private void DesignerMode()
        {
            HasWallets = true;
        }
    }
}