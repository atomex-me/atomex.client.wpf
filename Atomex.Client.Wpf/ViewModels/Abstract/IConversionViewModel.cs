using Atomex.Core;

namespace Atomex.Client.Wpf.ViewModels.Abstract
{
    public interface IConversionViewModel
    {
        CurrencyConfig FromCurrency { get; set; }
        CurrencyConfig ToCurrency { get; set; }
    }
}