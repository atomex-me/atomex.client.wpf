using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Atomix.Client.Wpf.Common;
using Atomix.Client.Wpf.Controls;
using Atomix.Client.Wpf.Properties;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Common;
using Atomix.Core;
using Atomix.Core.Entities;
using Serilog;

namespace Atomix.Client.Wpf.ViewModels
{
    public class ConversionConfirmationViewModel : BaseViewModel
    {
        public IAtomixApp App { get; }
        public IDialogViewer DialogViewer { get; }
        public Currency FromCurrency { get; set; }
        public Currency ToCurrency { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public string CurrencyFormat { get; set; }
        public string TargetCurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal FeeInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string TargetCurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }
        public decimal EstimatedPrice { get; set; }
        public string PriceFormat { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ?? (_backCommand = new Command(() =>
        {
            DialogViewer?.HideConversionConfirmationDialog();
        }));

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ?? (_nextCommand = new Command(Send));

#if DEBUG
        public ConversionConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public ConversionConfirmationViewModel(IAtomixApp app, IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        public void Show()
        {
            Navigation.Navigate(
                uri: Navigation.ConversionConfirmationAlias,
                context: this);
        }

        private async void Send()
        {
            try
            {
                Navigation.Navigate(uri: Navigation.SendingAlias);

                var error = await SendSwapAsync();

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
                        nextAction: () => {DialogViewer?.HideConversionConfirmationDialog();}));
            }
            catch (Exception e)
            {
                Navigation.Navigate(
                    uri: Navigation.MessageAlias,
                    context: MessageViewModel.Error(
                        text: "An error has occurred while sending swap.",
                        goBackPages: 2));

                Log.Error(e, "Swap error.");
            }
        }

        private async Task<Error> SendSwapAsync()
        {
            try
            {
                var account = App.Account;

                var requiredAmount = Amount + Fee;

                var fromWallets = (await account
                    .GetUnspentAddressesAsync(
                        currency: FromCurrency,
                        requiredAmount: requiredAmount))
                    .ToList();

                var refundWallet = await account
                    .GetRefundAddressAsync(FromCurrency, fromWallets);

                var toWallet = await account
                    .GetRedeemAddressAsync(ToCurrency);

                var symbol = Symbols.SymbolByCurrencies(FromCurrency, ToCurrency);
                var side = symbol.OrderSideForBuyCurrency(ToCurrency);
                var terminal = App.Terminal;
                var orderBook = terminal.GetOrderBook(symbol);
                var price = orderBook.EstimatedDealPrice(side, Amount);

                if (price == 0)
                    return new Error(Errors.NoLiquidity, Resources.CvNoLiquidity);

                var qty = Math.Round(AmountHelper.AmountToQty(side, Amount, price), symbol.QtyDigits);

                var order = new Order
                {
                    Symbol = symbol,
                    TimeStamp = DateTime.UtcNow,
                    Price = price,
                    Qty = qty,
                    Fee = Fee,
                    Side = side,
                    Type = OrderType.FillOrKill,
                    FromWallets = fromWallets.ToList(),
                    ToWallet = toWallet,
                    RefundWallet = refundWallet
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");
            
                return new Error(Errors.SwapError, Resources.CvConversionError);
            }

            return null;
        }

        private void DesignerMode()
        {
            FromCurrency = Currencies.Btc;
            ToCurrency = Currencies.Eth;
            FromCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(FromCurrency, false);
            ToCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(ToCurrency, false);
            CurrencyCode = FromCurrencyViewModel.CurrencyCode;
            CurrencyFormat = FromCurrencyViewModel.CurrencyFormat;
            TargetCurrencyCode = ToCurrencyViewModel.CurrencyCode;
            TargetCurrencyFormat = ToCurrencyViewModel.CurrencyFormat;
            BaseCurrencyCode = FromCurrencyViewModel.BaseCurrencyCode;
            BaseCurrencyFormat = FromCurrencyViewModel.BaseCurrencyFormat;

            PriceFormat = $"F{Symbols.EthBtc.Quote.Digits}";
            EstimatedPrice = 0.003m;

            Amount = 0.00001234m;
            AmountInBase = 10.23m;

            TargetAmount = Amount / EstimatedPrice;
            TargetAmountInBase = AmountInBase;

            Fee = 0.0001m;
            FeeInBase = 8.43m;
        }
    }
}