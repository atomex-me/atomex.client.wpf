using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Atomix.Client.Wpf.Common
{
    public class PageResolver : IPageResolver
    {
        private readonly Dictionary<string, Func<Page>> _pagesResolvers = new Dictionary<string, Func<Page>>();

        public Page GetPage(string alias)
        {
            return _pagesResolvers.TryGetValue(alias, out var resolver)
                ? resolver()
                : _pagesResolvers[Navigation.NotFoundAlias]();
        }

        public void AddResolver(string alias, Func<Page> resolver)
        {
            if (!_pagesResolvers.ContainsKey(alias))
                _pagesResolvers.Add(alias, resolver);
            else
                _pagesResolvers[alias] = resolver;
        }
    }
}