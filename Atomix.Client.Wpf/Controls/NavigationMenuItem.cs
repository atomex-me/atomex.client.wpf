using System.Windows;

namespace Atomix.Client.Wpf.Controls
{
    public class NavigationMenuItem : DependencyObject
    {
        public string Header { get; set; }
        public object Icon { get; set; }
        public object Content { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}