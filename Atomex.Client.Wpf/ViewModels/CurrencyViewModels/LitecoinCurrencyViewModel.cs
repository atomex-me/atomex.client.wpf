using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class LitecoinCurrencyViewModel : CurrencyViewModel
    {
        public LitecoinCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(new BitmapImage(new Uri(PathToImage("litecoin_90x90.png"))));
            IconMaskBrush       = new ImageBrush(new BitmapImage(new Uri(PathToImage("litecoin_mask.png"))));
            AccentColor         = Color.FromRgb(r: 191, g: 191, b: 191);
            AmountColor         = Color.FromRgb(r: 231, g: 231, b: 231);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("litecoin.png");
            LargeIconPath       = PathToImage("litecoin_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }
    }
}