using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Atomix.Client.Wpf.Controls;
using MahApps.Metro.Controls;

namespace Atomix.Client.Wpf.Common
{
    public static class WindowExtensions
    {
        public static Task ShowChildViewAsync(this MetroWindow window, ChildView view)
        {
            return ShowChildViewAsync<object>(window, view);
        }

        public static async Task<TResult> ShowChildViewAsync<TResult>(this MetroWindow window, ChildView view, bool overlay = true)
        {
            window.Dispatcher.VerifyAccess();

            if (overlay)
                await window.ShowOverlayAsync();

            var container = window.Template.FindName("PART_MetroActiveDialogContainer", window) as Grid;
            container = container ?? window.Template.FindName("PART_MetroInactiveDialogsContainer", window) as Grid;

            if (container == null)
                throw new InvalidOperationException("The provided child view can not be add, there is no container defined.");

            if (container.Children.Contains(view))
                throw new InvalidOperationException("The provided child view is already visible in the specified window.");

            return await ShowChildViewInternalAsync<TResult>(view, container);
        }

        public static Task ShowChildViewAsync(this Window window, ChildView view, Panel container)
        {
            return ShowChildViewAsync<object>(window, view, container);
        }

        public static Task<TResult> ShowChildViewAsync<TResult>(this Window window, ChildView view, Panel container)
        {
            window.Dispatcher.VerifyAccess();

            if (container == null)
                throw new InvalidOperationException("The provided child view can not add, there is no container defined.");

            if (container.Children.Contains(view))
                throw new InvalidOperationException("The provided child view is already visible in the specified window.");

            return ShowChildViewInternalAsync<TResult>(view, container);
        }

        public static void CloseAllChildViews(this Window window)
        {
            window.Dispatcher.VerifyAccess();

            var container = window.Template.FindName("PART_MetroActiveDialogContainer", window) as Grid;
            container = container ?? window.Template.FindName("PART_MetroInactiveDialogsContainer", window) as Grid;

            if (container == null)
                throw new InvalidOperationException("There is no container defined.");

            var children = container.Children
                .Cast<UIElement>()
                .ToList();

            foreach (var child in children)
            {
                if (child is ChildView childView)
                    childView.Close();
            }

            children.Clear();
        }

        private static async Task<TResult> ShowChildViewInternalAsync<TResult>(ChildView view, Panel container)
        {
            await AddViewToContainerAsync(view, container);
            return await OpenChildViewAsync<TResult>(view, container);
        }

        private static Task AddViewToContainerAsync(UIElement view, Panel container)
        {
            return Task.Factory.StartNew(() => view.Dispatcher.Invoke(() => container.Children.Add(view)));
        }

        private static Task<TResult> OpenChildViewAsync<TResult>(ChildView view, Panel container)
        {
            var tcs = new TaskCompletionSource<TResult>();

            void OnMouseDown(object sender, RoutedEventArgs args)
            {
                var elementOnTop = container.Children
                    .OfType<UIElement>()
                    .OrderBy(c => c.GetValue(Panel.ZIndexProperty))
                    .LastOrDefault();

                if (elementOnTop != null && !Equals(elementOnTop, view))
                {
                    var zIndex = (int)elementOnTop.GetValue(Panel.ZIndexProperty);
                    elementOnTop.SetCurrentValue(Panel.ZIndexProperty, zIndex - 1);
                    view.SetCurrentValue(Panel.ZIndexProperty, zIndex);
                }
            }

            void OnClosed(object sender, RoutedEventArgs args)
            {
                view.ClosingFinished -= OnClosed;
                view.PreviewMouseDown -= OnMouseDown;
                container.Children.Remove(view);

                var parentWindow = container.TryFindParent<MetroWindow>();
                if (parentWindow != null && parentWindow.IsOverlayVisible() && view.HideOverlayWhenClose)
                    parentWindow.HideOverlay();
                
                tcs.TrySetResult(view.ChildViewResult is TResult result ? result : default(TResult));
            }

            view.PreviewMouseDown += OnMouseDown;
            view.ClosingFinished += OnClosed;
            view.IsOpen = true;

            return tcs.Task;
        }
    }
}