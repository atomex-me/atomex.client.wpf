using System.Windows;

namespace Atomex.Client.Wpf.Controls
{
    public class NavigationMenuItemCollection : FreezableCollection<NavigationMenuItem>
    {
        protected override Freezable CreateInstanceCore()
        {
            return new NavigationMenuItemCollection();
        }
    }
}