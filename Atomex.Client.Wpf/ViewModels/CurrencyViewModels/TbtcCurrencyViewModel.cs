using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class TbtcCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TbtcCurrencyViewModel(Currency currency)
            : base(currency)
        {
            ChainCurrency = new Ethereum();
            Header = Currency.Description;
            IconBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tbtc_90x90_dark.png"))));
            IconMaskBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tbtc_mask.png"))));
            AccentColor = Color.FromRgb(r: 7, g: 82, b: 192);
            AmountColor = Color.FromRgb(r: 188, g: 212, b: 247);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("tbtc_dark.png");
            LargeIconPath = PathToImage("tbtc_90x90_dark.png");
            FeeName = Resources.SvGasLimit;
        }
    }
}