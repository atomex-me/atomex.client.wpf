using System;
using System.IO;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Wallet;
using Atomix.Wallet.Abstract;

namespace Atomix.Client.Wpf.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private IAccount _account;

        private IAtomixApp AtomixApp { get; }
        private IDialogViewer DialogViewer { get; }
        public UserSettings Settings { get; set; }

        public bool AutoSignOut
        {
            get => Settings?.AutoSignOut ?? true;
            set {
                Settings.AutoSignOut = value;
                OnPropertyChanged(nameof(AutoSignOut));
            }
        }

        private ICommand _applyCommand;
        public ICommand ApplyCommand => _applyCommand ?? (_applyCommand = new Command(Apply));

        public SettingsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public SettingsViewModel(IAtomixApp app, IDialogViewer dialogViewer)
        {
            AtomixApp = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            AtomixApp.AccountChanged += (sender, args) =>
            {
                _account = args.NewAccount;

                if (_account == null)
                    return;

                Settings = _account.UserSettings.Clone();
                OnPropertyChanged(nameof(Settings));
            };
        }

        private void Apply()
        {
            var unlockViewModel = new UnlockViewModel("wallet", password =>
            {
                var _ = Account.LoadFromFile(
                    pathToAccount: _account.Wallet.PathToWallet,
                    password: password,
                    currenciesProvider: AtomixApp.CurrenciesProvider,
                    symbolsProvider: AtomixApp.SymbolsProvider);

                var pathToUserSettings =
                    $"{Path.GetDirectoryName(_account.Wallet.PathToWallet)}/{Account.DefaultUserSettingsFileName}";

                _account.UseUserSettings(Settings);
                _account.UserSettings.SaveToFile(pathToUserSettings, password);
            });

            unlockViewModel.Unlocked += (sender, args) =>
            {
                DialogViewer?.HideUnlockDialog();
            };

            DialogViewer.ShowUnlockDialog(unlockViewModel);
        }

        private void DesignerMode()
        {
        }
    }
}