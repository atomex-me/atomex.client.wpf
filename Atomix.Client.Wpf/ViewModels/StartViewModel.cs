using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomix.Wallet;
using Atomix.Wallet.Abstract;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;

namespace Atomix.Client.Wpf.ViewModels
{
    public class StartViewModel : BaseViewModel
    {
        public IDialogViewer DialogViewer { get; set; }
        public IEnumerable<WalletInfo> Wallets { get; set; }
        public bool HasAvailableWallets => Wallets != null && Wallets.Any();

        public StartViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public StartViewModel(IDialogViewer dialogViewer)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            Wallets = WalletInfo.AvailableWallets();
        }

        private ICommand _createNewCommand;
        public ICommand CreateNewCommand => _createNewCommand ?? (_createNewCommand = new Command(() =>
        {
            DialogViewer?.ShowCreateWalletDialog(
                new CreateWalletViewModel(
                    scenario: CreateWalletScenario.CreateNewStages,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
        }));

        private ICommand _restoreByMnemonicCommand;
        public ICommand RestoreByMnemonicCommand => _restoreByMnemonicCommand ?? (_restoreByMnemonicCommand = new Command(() =>
        {
            DialogViewer?.ShowCreateWalletDialog(
                new CreateWalletViewModel(
                    scenario: CreateWalletScenario.UseMnemonicPhraseStages,
                    onAccountCreated: OnAccountCreated,
                    onCanceled: OnCanceled));
        }));

        private ICommand _restoreByFileCommand;
        public ICommand RestoreByFileCommand => _restoreByFileCommand ?? (_restoreByFileCommand = new Command(() =>
        {
        }));

        private void OnCanceled()
        {
            DialogViewer?.HideCreateWalletDialog(false);
        }

        private void OnAccountCreated(IAccount account)
        {
            App.AtomixApp.UseAccount(account, restartTerminal: true);

            DialogViewer?.HideCreateWalletDialog(false);
            DialogViewer?.HideStartDialog();
        }

        private ICommand _selectWalletCommand;
        public ICommand SelectWalletCommand => _selectWalletCommand ?? (_selectWalletCommand = new RelayCommand<WalletInfo>(info =>
        {
            ShowUnlockDialogAsync(info.Name, async password =>
            {
                IAccount account = null;

                await Task.Factory.StartNew(() => { account = new Account(info.Path, password); });

                App.AtomixApp.UseAccount(account, restartTerminal: true);

                DialogViewer?.HideStartDialog();
            });
        }));

        private Task ShowUnlockDialogAsync(string walletName, Func<SecureString, Task> unlockAction)
        {
            return DialogViewer?.ShowUnlockDialogAsync(CreateUnlockViewModel(walletName, unlockAction));
        }

        private UnlockViewModel CreateUnlockViewModel(string walletName, Func<SecureString, Task> unlockAction)
        {
            var viewModel = new UnlockViewModel(walletName, unlockAction);

            viewModel.Unlocked += (sender, args) => DialogViewer?.HideUnlockDialog();

            return viewModel;
        }

        private void DesignerMode()
        {
            Wallets = new List<WalletInfo>
            {
                new WalletInfo {Name = "default", Path = "wallets/default/"},
                new WalletInfo {Name = "market_maker", Path = "wallets/marketmaker/"},
                new WalletInfo {Name = "wallet1", Path = "wallets/default/"},
                new WalletInfo {Name = "my_first_wallet", Path = "wallets/marketmaker/"},
                new WalletInfo {Name = "mega_wallet", Path = "wallets/marketmaker/"}
            };
        }
    }
}