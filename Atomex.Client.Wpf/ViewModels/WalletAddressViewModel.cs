using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels
{
    public class WalletAddressViewModel
    {
        public WalletAddress WalletAddress { get; }
        public string Address => WalletAddress.Address;
        public decimal AvailableBalance => WalletAddress.AvailableBalance();
        public string CurrencyFormat => WalletAddress.Currency.Format;
        public bool IsFreeAddress { get; }

        public WalletAddressViewModel(WalletAddress walletAddress, bool isFreeAddress = false)
        {
            WalletAddress = walletAddress;
            IsFreeAddress = isFreeAddress;
        }
    }
}