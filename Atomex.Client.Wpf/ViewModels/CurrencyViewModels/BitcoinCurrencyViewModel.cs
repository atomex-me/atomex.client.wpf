using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class BitcoinCurrencyViewModel : CurrencyViewModel
    {
        public BitcoinCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(new BitmapImage(new Uri(PathToImage("bitcoin_90x90.png"))));
            IconMaskBrush       = new ImageBrush(new BitmapImage(new Uri(PathToImage("bitcoin_mask.png"))));
            AccentColor         = Color.FromRgb(r: 255, g: 148, b: 0);
            AmountColor         = Color.FromRgb(r: 255, g: 148, b: 0);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("bitcoin.png");
            LargeIconPath       = PathToImage("bitcoin_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }
    }
}