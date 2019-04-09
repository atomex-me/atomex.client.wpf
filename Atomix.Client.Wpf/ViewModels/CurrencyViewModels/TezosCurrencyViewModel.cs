using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Atomix.Blockchain.Tezos;
using Atomix.Client.Wpf.Properties;
using Atomix.Client.Wpf.ViewModels.Abstract;

namespace Atomix.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosCurrencyViewModel()
        {
            Currency = Currencies.Xtz;
            Header = Currency.Description;
            IconBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tezos.png"))));
            IconMaskBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tezos_mask.png"))));
            AccentColor = Color.FromRgb(r: 44, g: 125, b: 247);
            AmountColor = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("tezos.png");
            LargeIconPath = PathToImage("tezos_90x90.png");
            FeeName = Resources.SvMiningFee;
        }

        public override async Task UpdateAsync()
        {
            var transactions = (await Account
                .GetTransactionsAsync(Currency))
                .Cast<TezosTransaction>()
                .ToList();

            var confirmed = transactions
                .Where(t => t.IsConfirmed())
                .ToList();

            var confirmedBalance = confirmed.Aggregate(0m, (s, t) => s + t.AmountInXtz());

            var unconfirmed = transactions
                .Where(t => !t.IsConfirmed())
                .ToList();

            var unconfirmedBalance = unconfirmed.Aggregate(0m, (s, t) => s + t.AmountInXtz());

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