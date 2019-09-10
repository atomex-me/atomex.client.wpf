namespace Atomex.Client.Wpf.ViewModels.Abstract
{
    public interface IMenuSelector
    {
        int SelectedMenuIndex { get; }
        void SelectMenu(int index);       
    }
}