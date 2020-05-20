﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Atomex.Client.Wpf.Controls
{
    /// <summary>
    /// A static class to show ChildWindow's
    /// </summary>
    public static class ChildWindowManager
    {
        /// <summary>
        /// An enumeration to control the fill behavior of the behavior
        /// </summary>
        public enum OverlayFillBehavior
        {
            /// <summary>
            /// The overlay covers the full window
            /// </summary>
            FullWindow,

            /// <summary>
            /// The overlay covers only then window content, so the window title bar is useable
            /// </summary>
            WindowContent
        }

        /// <summary>
        /// Shows the given child window on the MetroWindow dialog container in an asynchronous way.
        /// </summary>
        /// <param name="window">The owning window with a container of the child window.</param>
        /// <param name="dialog">A child window instance.</param>
        /// <param name="overlayFillBehavior">The overlay fill behavior.</param>
        /// <returns>
        /// A task representing the operation.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// The provided child window can not add, the container can not be found.
        /// or
        /// The provided child window is already visible in the specified window.
        /// </exception>
        public static Task ShowChildWindowAsync(
            this Window window,
            ChildWindow dialog,
            OverlayFillBehavior overlayFillBehavior = OverlayFillBehavior.WindowContent)
        {
            return window.ShowChildWindowAsync<object>(dialog, overlayFillBehavior);
        }

        /// <summary>
        /// Shows the given child window on the MetroWindow dialog container in an asynchronous way.
        /// When the dialog was closed it returns a result.
        /// </summary>
        /// <param name="window">The owning window with a container of the child window.</param>
        /// <param name="dialog">A child window instance.</param>
        /// <param name="overlayFillBehavior">The overlay fill behavior.</param>
        /// <returns>
        /// A task representing the operation.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        /// The provided child window can not add, the container can not be found.
        /// or
        /// The provided child window is already visible in the specified window.
        /// </exception>
        public static Task<TResult> ShowChildWindowAsync<TResult>(
            this Window window,
            ChildWindow dialog,
            OverlayFillBehavior overlayFillBehavior = OverlayFillBehavior.WindowContent)
        {
            window.Dispatcher.VerifyAccess();

            var metroDialogContainer = window.Template.FindName("PART_MetroActiveDialogContainer", window) as Grid;
            metroDialogContainer ??= window.Template.FindName("PART_MetroInactiveDialogsContainer", window) as Grid;

            if (metroDialogContainer == null)
                throw new InvalidOperationException("The provided child window can not add, there is no container defined.");

            if (metroDialogContainer.Children.Contains(dialog))
                throw new InvalidOperationException("The provided child window is already visible in the specified window.");

            if (overlayFillBehavior == OverlayFillBehavior.WindowContent)
            {
                metroDialogContainer.SetValue(Grid.RowProperty, (int)metroDialogContainer.GetValue(Grid.RowProperty) + 1);
                metroDialogContainer.SetValue(Grid.RowSpanProperty, 1);
            }

            return ShowChildWindowInternalAsync<TResult>(dialog, metroDialogContainer);
        }

        /// <summary>
        /// Shows the given child window on the given container in an asynchronous way.
        /// When the dialog was closed it returns a result.
        /// </summary>
        /// <param name="window">The owning window with a container of the child window.</param>
        /// <param name="dialog">A child window instance.</param>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// The provided child window can not add, there is no container defined.
        /// or
        /// The provided child window is already visible in the specified window.
        /// </exception>
        public static Task ShowChildWindowAsync(
            this Window window,
            ChildWindow dialog,
            Panel container)
        {
            return window.ShowChildWindowAsync<object>(dialog, container);
        }

        /// <summary>
        /// Shows the given child window on the given container in an asynchronous way.
        /// </summary>
        /// <param name="window">The owning window with a container of the child window.</param>
        /// <param name="dialog">A child window instance.</param>
        /// <param name="container">The container.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// The provided child window can not add, there is no container defined.
        /// or
        /// The provided child window is already visible in the specified window.
        /// </exception>
        public static Task<TResult> ShowChildWindowAsync<TResult>(
            this Window window,
            ChildWindow dialog,
            Panel container)
        {
            window.Dispatcher.VerifyAccess();

            if (container == null)     
                throw new InvalidOperationException("The provided child window can not add, there is no container defined.");

            if (container.Children.Contains(dialog))
                throw new InvalidOperationException("The provided child window is already visible in the specified window.");

            return ShowChildWindowInternalAsync<TResult>(dialog, container);
        }

        private static Task<TResult> ShowChildWindowInternalAsync<TResult>(
            ChildWindow dialog,
            Panel container)
        {
            return AddDialogToContainerAsync(dialog, container)
                .ContinueWith(task => { return dialog.Dispatcher.Invoke(() => OpenDialogAsync<TResult>(dialog, container)); })
                .Unwrap();
        }

        private static Task AddDialogToContainerAsync(
            ChildWindow dialog,
            Panel container)
        {
            return Task.Factory.StartNew(() => dialog.Dispatcher.Invoke(new Action(() => container.Children.Add(dialog))));
        }

        private static Task<TResult> OpenDialogAsync<TResult>(ChildWindow dialog, Panel container)
        {
            //void DialogOnMouseUp(object sender, MouseButtonEventArgs args)
            //{
            //    var elementOnTop = container.Children.OfType<UIElement>().OrderBy(c => c.GetValue(Panel.ZIndexProperty)).LastOrDefault();
            //    if (elementOnTop != null && !Equals(elementOnTop, dialog))
            //    {
            //        var zIndex = (int) elementOnTop.GetValue(Panel.ZIndexProperty);
            //        elementOnTop.SetCurrentValue(Panel.ZIndexProperty, zIndex - 1);
            //        dialog.SetCurrentValue(Panel.ZIndexProperty, zIndex);
            //    }
            //}

            //dialog.PreviewMouseDown += DialogOnMouseUp;

            var tcs = new TaskCompletionSource<TResult>();

            void Handler(object sender, RoutedEventArgs args)
            {
                dialog.ClosingFinished -= Handler;
                //dialog.PreviewMouseDown -= DialogOnMouseUp;
                container.Children.Remove(dialog);
                tcs.TrySetResult(dialog.ChildWindowResult is TResult result ? result : default(TResult));
            }

            dialog.ClosingFinished += Handler;

            dialog.IsOpen = true;

            return tcs.Task;
        }

        public static void CloseAllChildWindows(this Window window)
        {
            window.Dispatcher.VerifyAccess();

            var container = window.Template.FindName("PART_MetroActiveDialogContainer", window) as Grid;
            container ??= window.Template.FindName("PART_MetroInactiveDialogsContainer", window) as Grid;

            if (container == null)
                throw new InvalidOperationException("There is no container defined.");

            var children = container.Children
                .Cast<UIElement>()
                .ToList();

            foreach (var child in children)
            {
                if (child is ChildWindow childView)
                    childView.Close();
            }

            children.Clear();
        }
    }
}