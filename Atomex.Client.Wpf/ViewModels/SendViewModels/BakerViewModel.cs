namespace Atomex.Client.Wpf.ViewModels.SendViewModels
{
    public class BakerViewModel : BaseViewModel
    {
        public string Logo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Fee { get; set; }
        public decimal StakingAvailable { get; set; }

        public bool IsFull => StakingAvailable <= 0;
    }
}
