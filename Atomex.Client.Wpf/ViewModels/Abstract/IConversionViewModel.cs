using Atomex.Core.Entities;

namespace Atomex.Client.Wpf.ViewModels.Abstract
{
    public interface IConversionViewModel
    {
        Currency FromCurrency { get; set; }
        Currency ToCurrency { get; set; }
    }
}