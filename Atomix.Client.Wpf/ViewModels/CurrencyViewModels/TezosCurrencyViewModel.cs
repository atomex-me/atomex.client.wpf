using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
            FeeName = Resources.SvGasLimit;
        }

        public override Task UpdateAsync()
        {
            return Task.CompletedTask;
        }
    }
}