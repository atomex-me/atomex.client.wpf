﻿using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels
{
    public class WalletAddressViewModel
    {
        public WalletAddress WalletAddress { get; }
        public string Address => WalletAddress.Address;
        public decimal AvailableBalance => WalletAddress.AvailableBalance();
        public string CurrencyFormat { get; }
        public bool IsFreeAddress { get; }

        public WalletAddressViewModel(
            WalletAddress walletAddress,
            string currencyFormat,
            bool isFreeAddress = false)
        {
            WalletAddress = walletAddress;
            CurrencyFormat = currencyFormat;
            IsFreeAddress = isFreeAddress;
        }
    }
}