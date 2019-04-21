using System;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Core.Entities;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels.SendViewModels
{
    public class SendConfirmationViewModel : BaseViewModel
    {
        public IDialogViewer DialogViewer { get; }
        public Currency Currency { get; set; }
        public string To { get; set; }
        public string CurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal FeePrice { get; set; }
        public decimal FeeAmount => Currency.GetFeeAmount(Fee, FeePrice);
        public decimal AmountInBase { get; set; }
        public decimal FeeInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(Navigation.Back));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(Send));

#if DEBUG
        public SendConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public SendConfirmationViewModel(IDialogViewer dialogViewer)
        {
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        private async void Send()
        {
            var account = App.AtomixApp.Account;

            try
            {
                Navigation.Navigate(uri: Navigation.SendingAlias);

                var error = await account
                    .SendAsync(Currency, To, Amount, Fee, FeePrice);

                if (error != null)
                {
                    Navigation.Navigate(
                        uri: Navigation.MessageAlias,
                        context: MessageViewModel.Error(
                            text: error.Description,
                            goBackPages: 2));
                    return;
                }

                Navigation.Navigate(
                    uri: Navigation.MessageAlias,
                    context: MessageViewModel.Success(
                        text: "Sending was successful",
                        nextAction: () => {DialogViewer?.HideSendDialog();}));
            }
            catch (Exception e)
            {
                Navigation.Navigate(
                    uri: Navigation.MessageAlias,
                    context: MessageViewModel.Error(
                        text: "An error has occurred while sending transaction.",
                        goBackPages: 2));

                Log.Error(e, "Transaction send error.");
            }
        }

        private void DesignerMode()
        {
            To = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";
            Amount = 0.00001234m;
            AmountInBase = 10.23m;
            Fee = 0.0001m;
            FeePrice = 1m;
            FeeInBase = 8.43m;
        }
    }
}