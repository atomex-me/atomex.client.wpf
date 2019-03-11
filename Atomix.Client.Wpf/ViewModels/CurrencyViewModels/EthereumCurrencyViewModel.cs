using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Atomix.Blockchain.Ethereum;
using Atomix.Client.Wpf.Properties;
using Atomix.Client.Wpf.ViewModels.Abstract;

namespace Atomix.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class EthereumCurrencyViewModel : CurrencyViewModel
    {
        public EthereumCurrencyViewModel()
        {
            Currency = Currencies.Eth;
            Header = Currency.Description;
            IconBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("ethereum.png"))));
            IconMaskBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("ethereum_mask.png"))));
            AccentColor = Color.FromRgb(r: 73, g: 114, b: 143);
            AmountColor = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("ethereum.png");
            LargeIconPath = PathToImage("ethereum_90x90.png");
            FeeName = Resources.SvGasLimit;
        }

        public override async Task UpdateAsync()
        {
            var transactions = (await Account
                .GetTransactionsAsync(Currency))
                .Cast<EthereumTransaction>()
                .ToList();

            var confirmed = transactions
                .Where(t => t.IsConfirmed())
                .ToList();

            var confirmedBalance = confirmed.Aggregate(0m, (s, t) => s + t.AmountInEth());

            var unconfirmed = transactions
                .Where(t => !t.IsConfirmed())
                .ToList();

            var unconfirmedBalance = unconfirmed.Aggregate(0m, (s, t) => s + t.AmountInEth());

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TotalAmount = confirmedBalance + unconfirmedBalance;
                OnPropertyChanged(nameof(TotalAmount));

                AvailableAmount = confirmedBalance;
                OnPropertyChanged(nameof(AvailableAmount));

                UnconfirmedAmount = unconfirmedBalance;
                OnPropertyChanged(nameof(UnconfirmedAmount));
                OnPropertyChanged(nameof(HasUnconfirmedAmount));

                UpdateQuotesInBaseCurrency(QuotesProvider);

            }, DispatcherPriority.Background);
        }
    }
}