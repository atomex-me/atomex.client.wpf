using System;
using System.Windows.Controls;

namespace Atomex.Client.Wpf.Common
{
    public interface IPageResolver
    {
        Page GetPage(string alias);
        void AddResolver(string alias, Func<Page> resolver);
    }
}