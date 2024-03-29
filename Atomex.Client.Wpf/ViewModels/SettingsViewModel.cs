﻿using System;
using System.IO;
using System.Windows.Input;

using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Common;
using Atomex.Wallet;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private IAccount _account;

        private IAtomexApp AtomexApp { get; }
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
        public ICommand ApplyCommand => _applyCommand ??= new Command(Apply);

        public SettingsViewModel()
        {
#if DEBUG
            if (Env.IsInDesignerMode())
                DesignerMode();
#endif
        }

        public SettingsViewModel(IAtomexApp app, IDialogViewer dialogViewer)
        {
            AtomexApp = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            AtomexApp.AtomexClientChanged += (sender, args) =>
            {
                _account = args.AtomexClient?.Account;

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
                    currenciesProvider: AtomexApp.CurrenciesProvider,
                    clientType: ClientType.Wpf);

                var pathToUserSettings =
                    $"{Path.GetDirectoryName(_account.Wallet.PathToWallet)}/{Account.DefaultUserSettingsFileName}";

                _account.UseUserSettings(Settings);
                _account.UserSettings.SaveToFile(pathToUserSettings, password);
            });

            unlockViewModel.Unlocked += (sender, args) =>
            {
                DialogViewer?.HideDialog(Dialogs.Unlock);
            };

            DialogViewer.ShowDialog(Dialogs.Unlock, unlockViewModel);
        }

        private void DesignerMode()
        {
        }
    }
}