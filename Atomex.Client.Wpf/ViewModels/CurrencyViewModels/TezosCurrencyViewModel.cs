using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class TezosCurrencyViewModel : CurrencyViewModel
    {
        public TezosCurrencyViewModel(CurrencyConfig currency)
            : base(currency)
        {
            Header              = Currency.Description;
            IconBrush           = DefaultIconBrush;
            IconMaskBrush       = DefaultIconMaskBrush;
            AccentColor         = Color.FromRgb(r: 44, g: 125, b: 247);
            AmountColor         = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = DefaultUnselectedIconBrush;
            IconPath            = PathToImage("tezos.png");
            LargeIconPath       = PathToImage("tezos_90x90.png");
            FeeName             = Resources.SvMiningFee;
        }

        public static Brush DefaultIconBrush  = new ImageBrush(new BitmapImage(new Uri(PathToImage("tezos_90x90.png"))));
        public static Brush DefaultIconMaskBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tezos_mask.png"))));
        public static Brush DefaultUnselectedIconBrush = Brushes.White;

    }
}