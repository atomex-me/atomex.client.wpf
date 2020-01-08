using System.Windows.Navigation;
using Atomex.Client.Wpf.Controls;

namespace Atomex.Client.Wpf.Views
{
    public partial class FrameView : ChildWindow
    {
        public NavigationService NavigationService => Frame.NavigationService;

        public FrameView()
        {
            InitializeComponent();
        }
    }
}