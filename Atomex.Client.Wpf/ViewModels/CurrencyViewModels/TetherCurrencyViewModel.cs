using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Atomex.Client.Wpf.Properties;
using Atomex.Client.Wpf.ViewModels.Abstract;
using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.CurrencyViewModels
{
    public class TetherCurrencyViewModel : CurrencyViewModel
    {
        public decimal AvailableAmountInChainCurrency { get; set; }

        public TetherCurrencyViewModel(Currency currency)
            : base(currency)
        {
            ChainCurrency = new Ethereum();
            Header = Currency.Description;
            IconBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tether_90x90.png"))));
            IconMaskBrush = new ImageBrush(new BitmapImage(new Uri(PathToImage("tether_mask.png"))));
            AccentColor = Color.FromRgb(r: 0, g: 162, b: 122);
            AmountColor = Color.FromRgb(r: 183, g: 208, b: 225);
            UnselectedIconBrush = Brushes.White;
            IconPath = PathToImage("tether.png");
            LargeIconPath = PathToImage("tether_90x90.png");
            FeeName = Resources.SvGasLimit;
        }
    }
}