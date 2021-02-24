using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class KusdCurrencyViewModel : CurrencyViewModel
    {
        public KusdCurrencyViewModel(Currency currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = new ImageBrush(new BitmapImage(new Uri(PathToImage("kusd_90x90.png"))));
            IconMaskBrush       = new ImageBrush(new BitmapImage(new Uri(PathToImage("kusd_mask.png"))));
            AccentColor         = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath            = PathToImage("kusd.png");
            LargeIconPath       = PathToImage("kusd_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }
    }
}