using System;
using System.Windows.Input;
using Atomex.Wallet.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using System.Diagnostics;

namespace Atomex.Client.Wpf.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }

        public StartViewModel()
        {
        }

        public StartViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        private ICommand _myWalletsCommand;
        public ICommand MyWalletsCommand => _myWalletsCommand ?? (_myWalletsCommand = new Command(() =>
        {
            DialogViewer?.ShowMyWalletsDialog(
                new MyWalletsViewModel(App, DialogViewer));
        }));

        private ICommand _createNewCommand;
        public ICommand CreateNewCommand => _createNewCommand ?? (_createNewCommand = new Command(() =>
        {
            DialogViewer?.ShowCreateWalletDialog(
                new CreateWalletViewModel(
                    app: App,
                    scenario: CreateWalletScenario.CreateNew,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
        }));

        private ICommand _restoreByMnemonicCommand;
        public ICommand RestoreByMnemonicCommand => _restoreByMnemonicCommand ?? (_restoreByMnemonicCommand = new Command(() =>
        {
            DialogViewer?.ShowCreateWalletDialog(
                new CreateWalletViewModel(
                    app: App,
                    scenario: CreateWalletScenario.Restore,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
        }));


        private ICommand _twitterCommand;
        public ICommand TwitterCommand => _twitterCommand ?? (_twitterCommand = new Command(() =>
        {
            Process.Start("https://twitter.com/atomex_official");
        }));

        private ICommand _telegramCommand;
        public ICommand TelegramCommand => _telegramCommand ?? (_telegramCommand = new Command(() =>
        {
            Process.Start("tg://resolve?domain=atomex_official");
        }));

        private ICommand _githubCommand;
        public ICommand GithubCommand => _githubCommand ?? (_githubCommand = new Command(() =>
        {
            Process.Start("https://github.com/atomex-me");
        }));

        private void OnCanceled()
        {
            DialogViewer?.HideCreateWalletDialog();
        }

        private void OnAccountCreated(IAccount account)
        {
            App.UseAccount(account, restartTerminal: true);

            DialogViewer?.HideCreateWalletDialog();
            DialogViewer?.HideStartDialog();
        }
    }
}