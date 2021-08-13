using System;
using System.Windows.Input;

using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Blockchain.Tezos.Internal;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Core;
using Atomex.Wallet;
using Atomex.Wallet.Tezos;

namespace Atomex.Client.Wpf.ViewModels
{
    public class DelegateConfirmationViewModel : BaseViewModel
    {
        private IAtomexApp App { get; }
        public IDialogViewer DialogViewer { get; }
        public TezosConfig Currency { get; set; }
        
        public WalletAddress WalletAddress { get; set; }
        public bool UseDefaultFee { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Fee { get; set; }
        public bool IsAmountLessThanMin { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            DialogViewer.Back(Dialogs.Delegate);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(Send);

        private readonly Action _onDelegate;

#if DEBUG
        public DelegateConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public DelegateConfirmationViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer,
            Action onDelegate = null)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));

            _onDelegate = onDelegate;
        }

        private async void Send()
        {
            var wallet     = (HdWallet) App.Account.Wallet;
            var keyStorage = wallet.KeyStorage;
            var tezos      = Currency;

            var tezosAccount = App.Account
                .GetCurrencyAccount<TezosAccount>("XTZ");

            try
            {
                DialogViewer.PushPage(Dialogs.Delegate, Pages.Delegating);

                await tezosAccount.AddressLocker
                    .LockAsync(WalletAddress.Address);

                var tx = new TezosTransaction
                {
                    StorageLimit      = Currency.StorageLimit,
                    GasLimit          = Currency.GasLimit,
                    From              = WalletAddress.Address,
                    To                = To,
                    Fee               = Fee.ToMicroTez(),
                    Currency          = Currency.Name,
                    CreationTime      = DateTime.UtcNow,

                    UseRun            = true,
                    UseOfflineCounter = true,
                    OperationType     = OperationType.Delegation
                };

                using var securePublicKey = App.Account.Wallet.GetPublicKey(
                    currency: Currency,
                    keyIndex: WalletAddress.KeyIndex,
                    keyType: WalletAddress.KeyType);

                await tx.FillOperationsAsync(
                    securePublicKey: securePublicKey,
                    tezosConfig: Currency,
                    headOffset: TezosConfig.HeadOffset);

                var signResult = await tx
                    .SignAsync(keyStorage, WalletAddress, default);

                if (!signResult)
                {
                    Log.Error("Transaction signing error");

                    DialogViewer.PushPage(Dialogs.Delegate, Pages.Message, MessageViewModel.Error(
                        text: "Transaction signing error",
                        backAction: BackToConfirmation));

                    return;
                }

                var result = await tezos.BlockchainApi
                    .TryBroadcastAsync(tx);

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
            finally
            {
                tezosAccount.AddressLocker.Unlock(WalletAddress.Address);
            }
        }

        private void BackToConfirmation()
        {
            DialogViewer.Back(Dialogs.Delegate); // to delegating view
            DialogViewer.Back(Dialogs.Delegate); // to confirmation view
        }

        private void DesignerMode()
        {
            From      = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            To        = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Fee       = 0.0001m;
            FeeInBase = 8.43m;
        }
    }
}