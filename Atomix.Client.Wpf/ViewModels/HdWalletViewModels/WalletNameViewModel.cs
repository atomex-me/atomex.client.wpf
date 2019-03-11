using System;
using System.IO;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Properties;

namespace Atomix.Client.Wpf.ViewModels.HdWalletViewModels
{
    public class WalletNameViewModel : StageViewModel
    {
        public override event Action<object> OnNext;
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

            var pathToWallet = $"{WalletInfo.CurrentWalletDirectory}\\{WalletName}\\{WalletInfo.DefaultWalletFileName}";

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

            OnNext?.Invoke(pathToWallet);
        }
    }
}