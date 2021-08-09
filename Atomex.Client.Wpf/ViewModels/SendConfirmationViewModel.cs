using System;
using System.Windows.Input;

using Serilog;

using Atomex.Blockchain.Tezos;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Core;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.Tezos;
using Atomex.Client.Wpf.ViewModels.SendViewModels;

namespace Atomex.Client.Wpf.ViewModels
{
    public class SendConfirmationViewModel : BaseViewModel
    {
        private readonly int _dialogId;
        private readonly IDialogViewer _dialogViewer;

        public CurrencyConfig Currency { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal FeePrice { get; set; }
        public decimal FeeAmount => Currency.GetFeeAmount(Fee, FeePrice);
        public bool UseDeafultFee { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }
        public string FeeCurrencyCode { get; set; }
        public string FeeCurrencyFormat { get; set; }
        public string TokenContract { get; set; }
        public decimal TokenId { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            _dialogViewer.Back(_dialogId);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(Send);

#if DEBUG
        public SendConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public SendConfirmationViewModel(IDialogViewer dialogViewer, int dialogId)
        {
            _dialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
            _dialogId = dialogId;
        }

        private async void Send()
        {


            try
            {
                _dialogViewer.PushPage(_dialogId, Pages.Sending);

                Error error;

                if (From != null && TokenContract != null) // tezos token sending
                {
                    var tezosAccount = App.AtomexApp.Account
                        .GetCurrencyAccount<TezosAccount>(TezosConfig.Xtz);

                    var tokenAddress = await TezosTokensSendViewModel.GetTokenAddressAsync(
                        App.AtomexApp.Account,
                        From,
                        TokenContract,
                        TokenId);

                    if (tokenAddress.Currency == "FA12")
                    {
                        var tokenAccount = App.AtomexApp.Account
                            .GetTezosTokenAccount<Fa12Account>("FA12", TokenContract, TokenId);

                        error = await tokenAccount
                            .SendAsync(new WalletAddress[] { tokenAddress }, To, Amount, Fee, FeePrice, UseDeafultFee);
                    }
                    else
                    {
                        var tokenAccount = App.AtomexApp.Account
                            .GetTezosTokenAccount<Fa2Account>("FA2", TokenContract, TokenId);

                        var decimals = tokenAddress.TokenBalance.Decimals;
                        var amount = (int)(Amount * (decimal)Math.Pow(10, decimals));
                        var fee = (int)Fee.ToMicroTez();

                        error = await tokenAccount.SendAsync(
                            from: From,
                            to: To,
                            amount: amount,
                            tokenContract: TokenContract,
                            tokenId: (int)TokenId,
                            fee: fee,
                            useDefaultFee: UseDeafultFee);
                    }
                }
                else
                {
                    var account = App.AtomexApp.Account
                        .GetCurrencyAccount<ILegacyCurrencyAccount>(Currency.Name);

                    error = await account
                        .SendAsync(To, Amount, Fee, FeePrice, UseDeafultFee);
                }

                if (error != null)
                {
                    _dialogViewer.PushPage(_dialogId, Pages.Message, MessageViewModel.Error(
                        text: error.Description,
                        backAction: BackToConfirmation));

                    return;
                }

                _dialogViewer.PushPage(_dialogId, Pages.Message, MessageViewModel.Success(
                    text: "Sending was successful",
                    nextAction: () => { _dialogViewer.HideDialog(_dialogId); }));
            }
            catch (Exception e)
            {
                _dialogViewer.PushPage(_dialogId, Pages.Message, MessageViewModel.Error(
                    text: "An error has occurred while sending transaction.",
                    backAction: BackToConfirmation));

                Log.Error(e, "Transaction send error.");
            }
        }

        private void BackToConfirmation()
        {
            _dialogViewer.Back(_dialogId); // to sending view
            _dialogViewer.Back(_dialogId); // to confirmation view
        }

        private void DesignerMode()
        {
            To            = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Amount        = 0.00001234m;
            AmountInBase  = 10.23m;
            Fee           = 0.0001m;
            FeePrice      = 1m;
            FeeInBase     = 8.43m;
            UseDeafultFee = true;
        }
    }
}