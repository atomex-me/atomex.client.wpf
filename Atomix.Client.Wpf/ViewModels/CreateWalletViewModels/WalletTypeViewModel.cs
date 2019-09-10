using Atomix.Core;

namespace Atomix.Client.Wpf.ViewModels.CreateWalletViewModels
{
    public class WalletTypeViewModel : StepViewModel
    {
        public static Network[] Networks { get; } = {
            Network.MainNet,
            Network.TestNet
        };

        private Network _network;
        public Network Network
        {
            get => _network;
            set { _network = value; OnPropertyChanged(nameof(Network)); }
        }

        public WalletTypeViewModel()
        {
            Network = Network.MainNet;
        }

        public override void Next()
        {
            RaiseOnNext(new StepData
            {
                Network = Network
            });
        }
    }
}