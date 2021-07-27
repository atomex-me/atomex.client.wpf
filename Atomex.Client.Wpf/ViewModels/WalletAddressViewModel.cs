namespace Atomex.Client.Wpf.ViewModels
{
    public class WalletAddressViewModel
    {
        public string Address { get; set; }
        public bool HasActivity { get; set; }
        public decimal AvailableBalance { get; set; }
        public string CurrencyFormat { get; set; }
        public string CurrencyCode { get; set; }
        public bool IsFreeAddress { get; set; }
        public bool ShowTokenBalance { get; set; }
        public decimal TokenBalance { get; set; }
        public string TokenFormat { get; set; }
        public string TokenCode { get; set; }

    }
}