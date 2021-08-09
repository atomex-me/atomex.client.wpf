using System.Windows.Media;

namespace Atomex.Client.Wpf.ViewModels.WalletViewModels
{
    public interface IWalletViewModel
    {
        Brush Background { get; }
        string Header { get; }
        bool IsSelected { get; set; }
        Brush OpacityMask { get; }
    }
}