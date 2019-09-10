using System;
using System.IO;
using System.Windows.Input;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Properties;

namespace Atomex.Client.Wpf.ViewModels.CreateWalletViewModels
{
    public class WalletNameViewModel : StepViewModel
    {
        private StepData StepData { get; set; }

        public string WalletName { get; set; }

        private string _warning;
        public string Warning
        {
            get => _warning;
            set { _warning = value; OnPropertyChanged(nameof(Warning)); }
        }

        private ICommand _textChangedCommand;
        public ICommand TextChangedCommand => _textChangedCommand ?? (_textChangedCommand = new Command(TextChanged));

        private void TextChanged()
        {
            Warning = string.Empty;
        }

        public override void Initialize(
            object arg)
        {
            StepData = (StepData) arg;
        }

        public override void Next()
        {
            if (string.IsNullOrEmpty(WalletName)) {
                Warning = Resources.CwvEmptyWalletName;
                return;
            }

            if (WalletName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 ||
                WalletName.IndexOf('.') != -1) {
                Warning = Resources.CwvInvalidWalletName;
                return;
            }

            var pathToWallet = $"{WalletInfo.CurrentWalletDirectory}/{WalletName}/{WalletInfo.DefaultWalletFileName}";

            try
            {
                var _ = Path.GetFullPath(pathToWallet);
            }
            catch (Exception)
            {
                Warning = Resources.CwvInvalidWalletName;
                return;
            }

            if (File.Exists(pathToWallet)) {
                Warning = Resources.CwvWalletAlreadyExists;
                return;
            }

            StepData.PathToWallet = pathToWallet;

            RaiseOnNext(StepData);
        }
    }
}