using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Atomex.Client.Wpf.ViewModels.Abstract;

namespace Atomex.Client.Wpf.Views
{
    public partial class WalletView : UserControl
    {
        public WalletView()
        {
            InitializeComponent();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var items in new[] {e.AddedItems, e.RemovedItems})
            {
                foreach (var selectedItem in items)
                {
                    if (!(selectedItem is IExpandable expandableModel))
                        continue;

                    if (expandableModel.IsExpanded)
                    {
                        expandableModel.IsExpanded = false;
                        var row = GetRowByItem(selectedItem);

                        if (row != null)
                            row.DetailsVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        expandableModel.IsExpanded = true;

                        var row = GetRowByItem(selectedItem);

                        if (row != null)
                            row.DetailsVisibility = Visibility.Visible;
                    }
                }
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DgTransactions.SelectedItems.Count != 1)
                return;

            var row = GetRowByItem(DgTransactions.SelectedItem);

            if (row.IsMouseOver)
                row.IsSelected = false;
        }

        private DataGridRow GetRowByItem(object item)
        {
            return DgTransactions.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
        }
    }
}