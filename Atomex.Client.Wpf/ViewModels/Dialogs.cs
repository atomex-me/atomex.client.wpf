namespace Atomex.Client.Wpf.ViewModels
{
    public static class Dialogs
    {
        public const int Message = 1;
        public const int Start = 2;
        public const int CreateWallet = 3;
        public const int MyWallets = 4;
        public const int Receive = 5;
        public const int Unlock = 6;
        public const int Send = 7;
        public const int Delegate = 20;
        public const int Convert = 30;
        public const int Addresses = 40;
    }

    public static class Pages
    {
        public const int Message = 1;
        
        public const int SendBitcoinBased = 11;
        public const int SendEthereum = 12;
        public const int SendTezos = 13;
        public const int SendErc20 = 14;
        public const int SendFa12 = 15;
        public const int SendTezosTokens = 16;
        public const int SendConfirmation = 18;
        public const int Sending = 19;

        public const int Delegate = 20;
        public const int DelegateConfirmation = 21;
        public const int Delegating = 22;
        public const int ConversionConfirmation = 30;
    }
}