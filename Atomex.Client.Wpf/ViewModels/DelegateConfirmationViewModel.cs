using System;
using System.Windows.Input;
using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Core.Entities;
using Atomex.Wallet;
using Serilog;

namespace Atomex.Client.Wpf.ViewModels
{
    public class DelegateConfirmationViewModel : BaseViewModel
    {
        public IDialogViewer DialogViewer { get; }
        public Currency Currency { get; set; }
        
        public TezosTransaction Tx { get; set; }
        public WalletAddress WalletAddress { get; set; }
        public bool UseDefaultFee { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Fee { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer.Back(Dialogs.Delegate);
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(Send));

        private readonly Action _onDelegate;

#if DEBUG
        public DelegateConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public DelegateConfirmationViewModel(
            IDialogViewer dialogViewer,
            Action onDelegate = null)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            _onDelegate = onDelegate;
        }

        private async void Send()
        {
            var wallet = (HdWallet) App.AtomexApp.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var tezos = (Tezos)Currency;
            
            try
            {
                DialogViewer.PushPage(Dialogs.Delegate, Pages.Delegating);

                var signResult = await Tx
                    .SignDelegationOperationAsync(keyStorage, WalletAddress, default);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");

                    DialogViewer.PushPage(Dialogs.Delegate, Pages.Message, MessageViewModel.Error(
                        text: "Transaction signing error",
                        backAction: BackToConfirmation));

                    return;
                }

                var result = await tezos.BlockchainApi
                    .TryBroadcastAsync(Tx);

                if (result.Error != null)
                {
                    DialogViewer.PushPage(Dialogs.Delegate, Pages.Message, MessageViewModel.Error(
                        text: result.Error.Description,
                        backAction: BackToConfirmation));

                    return;
                }

                DialogViewer.PushPage(Dialogs.Delegate, Pages.Message, MessageViewModel.Success(
                    text: $"Successful delegation!",
                    tezos.TxExplorerUri,
                    result.Value,
                    nextAction: () =>
                    {
                        DialogViewer.HideDialog(Dialogs.Delegate);

                        _onDelegate?.Invoke();
                    }));
            }
            catch (Exception e)
            {
                DialogViewer.PushPage(Dialogs.Delegate, Pages.Message, MessageViewModel.Error(
                    text: "An error has occurred while delegation.",
                    backAction: BackToConfirmation));

                Log.Error(e, "delegation send error.");
            }
        }

        private void BackToConfirmation()
        {
            DialogViewer.Back(Dialogs.Delegate); // to delegating view
            DialogViewer.Back(Dialogs.Delegate); // to confirmation view
        }

        private void DesignerMode()
        {
            From = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            To = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Fee = 0.0001m;
            //FeePrice = 1m;
            FeeInBase = 8.43m;
        }
    }
}