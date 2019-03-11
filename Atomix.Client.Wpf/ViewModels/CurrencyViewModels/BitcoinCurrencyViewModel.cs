using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Atomix.Client.Wpf.Properties;

namespace Atomix.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : BitcoinBasedCurrencyViewModel
    {
        public BitcoinCurrencyViewModel()
        {
            Currency = Currencies.Btc;
            Header = Currency.Description;
            IconBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("bitcoin.png"))));
            IconMaskBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("bitcoin_mask.png"))));
            AccentColor = Color.FromRgb(r: 255, g: 148, b: 0);
            AmountColor = Color.FromRgb(r: 255, g: 148, b: 0);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("bitcoin.png");
            LargeIconPath = PathToImage("bitcoin_90x90.png");
            FeeName = Resources.SvMiningFee;
        }
    }
}