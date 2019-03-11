using Atomix.Core.Entities;

namespace Atomix.Client.Wpf.ViewModels.Abstract
{
    public interface IConversionViewModel
    {
        Currency FromCurrency { get; set; }
        Currency ToCurrency { get; set; }
    }
}