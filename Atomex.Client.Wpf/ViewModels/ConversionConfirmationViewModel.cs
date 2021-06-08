using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Serilog;

using Atomex.Blockchain.Abstract;
using Atomex.Client.Wpf.Common;
using Atomex.Client.Wpf.Controls;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Client.Wpf.ViewModels.CurrencyViewModels;
using Atomex.Common;
using Atomex.Core;
using Atomex.Wallet.Abstract;

namespace Atomex.Client.Wpf.ViewModels
{
    public class ConversionConfirmationViewModel : BaseViewModel
    {
        public event EventHandler OnSuccess;

        private static TimeSpan SwapTimeout = TimeSpan.FromSeconds(60);
        private static TimeSpan SwapCheckInterval = TimeSpan.FromSeconds(3);

        private IAtomexApp App { get; }
        private IDialogViewer DialogViewer { get; }
        public CurrencyConfig FromCurrency { get; set; }
        public CurrencyConfig ToCurrency { get; set; }
        public CurrencyViewModel FromCurrencyViewModel { get; set; }
        public CurrencyViewModel ToCurrencyViewModel { get; set; }
        public string PriceFormat { get; set; }
        public string CurrencyFormat { get; set; }
        public string TargetCurrencyFormat { get; set; }
        public string BaseCurrencyFormat { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInBase { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal TargetAmountInBase { get; set; }
        public string CurrencyCode { get; set; }
        public string TargetCurrencyCode { get; set; }
        public string BaseCurrencyCode { get; set; }

        public string FromFeeCurrencyCode { get; set; }
        public string FromFeeCurrencyFormat { get; set; }
        public string TargetFeeCurrencyCode { get; set; }
        public string TargetFeeCurrencyFormat { get; set; }

        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedOrderPrice { get; set; }
        public decimal EstimatedPaymentFee { get; set; }
        public decimal EstimatedPaymentFeeInBase { get; set; }
        public decimal EstimatedRedeemFee { get; set; }
        public decimal EstimatedRedeemFeeInBase { get; set; }
        public decimal EstimatedMakerNetworkFee { get; set; }
        public decimal EstimatedMakerNetworkFeeInBase { get; set; }
        public decimal EstimatedTotalNetworkFeeInBase { get; set; }

        public decimal RewardForRedeem { get; set; }
        public decimal RewardForRedeemInBase { get; set; }
        public bool HasRewardForRedeem { get; set; }

        private ICommand _backCommand;
        public ICommand BackCommand => _backCommand ??= new Command(() =>
        {
            DialogViewer.HideDialog(Dialogs.Convert);
        });

        private ICommand _nextCommand;
        public ICommand NextCommand => _nextCommand ??= new Command(Send);

#if DEBUG
        public ConversionConfirmationViewModel()
        {
            if (Env.IsInDesignerMode())
                DesignerMode();
        }
#endif
        public ConversionConfirmationViewModel(
            IAtomexApp app,
            IDialogViewer dialogViewer)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            DialogViewer = dialogViewer ?? throw new ArgumentNullException(nameof(dialogViewer));
        }

        private async void Send()
        {
            try
            {
                DialogViewer.PushPage(Dialogs.Convert, Pages.Sending);

                var error = await ConvertAsync();

                if (error != null)
                {
                    if (error.Code == Errors.PriceHasChanged)
                    {
                        DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Message(
                            title: Resources.SvFailed,
                            text: error.Description,
                            backAction: BackToConfirmation));
                    }
                    else
                    {
                        DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Error(
                            text: error.Description,
                            backAction: BackToConfirmation));
                    }

                    return;
                }

                DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Success(
                    text: Resources.SvOrderMatched,
                    nextAction: () => {
                        DialogViewer?.HideDialog(Dialogs.Convert);
                        OnSuccess?.Invoke(this, EventArgs.Empty);
                    }),
                    closeAction: () => OnSuccess?.Invoke(this, EventArgs.Empty));
            }
            catch (Exception e)
            {
                DialogViewer.PushPage(Dialogs.Convert, Pages.Message, MessageViewModel.Error(
                    text: "An error has occurred while sending swap.",
                    backAction: BackToConfirmation));

                Log.Error(e, "Swap error.");
            }
        }

        private async Task<Error> ConvertAsync()
        {
            try
            {
                var account = App.Account;
                var currencyAccount = account
                    .GetCurrencyAccount<ILegacyCurrencyAccount>(FromCurrency.Name);

                var fromWallets = (await currencyAccount
                    .GetUnspentAddressesAsync(
                        toAddress: null,
                        amount: Amount,
                        fee: 0,
                        feePrice: await FromCurrency.GetDefaultFeePriceAsync(),
                        feeUsagePolicy: FeeUsagePolicy.EstimatedFee,
                        addressUsagePolicy: AddressUsagePolicy.UseMinimalBalanceFirst,
                        transactionType: BlockchainTransactionType.SwapPayment))
                    .ToList();

                if (Amount == 0)
                    return new Error(Errors.SwapError, Resources.CvZeroAmount);

                if (Amount > 0 && !fromWallets.Any())
                    return new Error(Errors.SwapError, Resources.CvInsufficientFunds);

                var symbol = App.SymbolsProvider
                    .GetSymbols(App.Account.Network)
                    .SymbolByCurrencies(FromCurrency, ToCurrency);

                var baseCurrency = App.Account.Currencies.GetByName(symbol.Base);
                var side         = symbol.OrderSideForBuyCurrency(ToCurrency);
                var terminal     = App.Terminal;
                var price        = EstimatedPrice;
                var orderPrice   = EstimatedOrderPrice;

                if (price == 0)
                    return new Error(Errors.NoLiquidity, Resources.CvNoLiquidity);

                var qty = AmountHelper.AmountToQty(side, Amount, price, baseCurrency.DigitsMultiplier);

                if (qty < symbol.MinimumQty)
                {
                    var minimumAmount = AmountHelper.QtyToAmount(side, symbol.MinimumQty, price, FromCurrency.DigitsMultiplier);
                    var message = string.Format(CultureInfo.InvariantCulture, Resources.CvMinimumAllowedQtyWarning, minimumAmount, FromCurrency.Name);

                    return new Error(Errors.SwapError, message);
                }

                var order = new Order
                {
                    Symbol          = symbol.Name,
                    TimeStamp       = DateTime.UtcNow,
                    Price           = orderPrice,
                    Qty             = qty,
                    Side            = side,
                    Type            = OrderType.FillOrKill,
                    FromWallets     = fromWallets.ToList(),
                    MakerNetworkFee = EstimatedMakerNetworkFee
                };

                await order.CreateProofOfPossessionAsync(account);

                terminal.OrderSendAsync(order);

                // wait for swap confirmation
                var timeStamp = DateTime.UtcNow;

                while (DateTime.UtcNow < timeStamp + SwapTimeout)
                {
                    await Task.Delay(SwapCheckInterval);

                    var currentOrder = terminal.Account.GetOrderById(order.ClientOrderId);

                    if (currentOrder == null)
                        continue;

                    if (currentOrder.Status == OrderStatus.Pending)
                        continue;

                    if (currentOrder.Status == OrderStatus.PartiallyFilled || currentOrder.Status == OrderStatus.Filled)
                    {
                        var swap = (await terminal.Account
                            .GetSwapsAsync())
                            .FirstOrDefault(s => s.OrderId == currentOrder.Id);

                        if (swap == null)
                            continue;

                        return null;
                    }

                    if (currentOrder.Status == OrderStatus.Canceled)
                        return new Error(Errors.PriceHasChanged, Resources.SvPriceHasChanged);

                    if (currentOrder.Status == OrderStatus.Rejected)
                        return new Error(Errors.OrderRejected, Resources.SvOrderRejected);
                }

                return new Error(Errors.TimeoutReached, Resources.SvTimeoutReached);
            }
            catch (Exception e)
            {
                Log.Error(e, "Conversion error");
            
                return new Error(Errors.SwapError, Resources.CvConversionError);
            }
        }

        private void BackToConfirmation()
        {
            DialogViewer.Back(Dialogs.Convert); // to sending view
            DialogViewer.Back(Dialogs.Convert); // to confirmation view
        }

        private void DesignerMode()
        {
            var currencies = DesignTime.Currencies.ToList();

            FromCurrency = currencies[0];
            ToCurrency = currencies[1];
            FromCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(FromCurrency, false);
            ToCurrencyViewModel = CurrencyViewModelCreator.CreateViewModel(ToCurrency, false);

            PriceFormat = $"F{FromCurrency.Digits}";
            CurrencyCode = FromCurrencyViewModel.CurrencyCode;
            CurrencyFormat = FromCurrencyViewModel.CurrencyFormat;
            TargetCurrencyCode = ToCurrencyViewModel.CurrencyCode;
            TargetCurrencyFormat = ToCurrencyViewModel.CurrencyFormat;
            BaseCurrencyCode = FromCurrencyViewModel.BaseCurrencyCode;
            BaseCurrencyFormat = FromCurrencyViewModel.BaseCurrencyFormat;

            EstimatedPrice = 0.003m;

            Amount = 0.00001234m;
            AmountInBase = 10.23m;

            TargetAmount = Amount / EstimatedPrice;
            TargetAmountInBase = AmountInBase;

            EstimatedPaymentFee = 0.0001904m;
            EstimatedPaymentFeeInBase = 0.22m;

            EstimatedRedeemFee = 0.001m;
            EstimatedRedeemFeeInBase = 0.11m;
        }
    }
}