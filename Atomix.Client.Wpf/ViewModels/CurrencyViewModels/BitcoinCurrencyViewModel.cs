using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Atomix.Client.Wpf.Properties;
using Atomix.Client.Wpf.ViewModels.Abstract;
using Atomix.Core.Entities;

namespace Atomix.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : CurrencyViewModel
    {
        public BitcoinCurrencyViewModel(Currency currency)
            : base(currency)
        {
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