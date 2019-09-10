using System.Collections.Generic;

namespace Atomex.Client.Wpf.Common.Msi
{
    abstract class PropsCollection
    {
        readonly Dictionary<string, string> Props = new Dictionary<string, string>();

        public string this[string propName]
        {
            get
            {
                if (!Props.ContainsKey(propName))
                    Props[propName] = GetProperty(propName);

                return Props[propName];
            }
        }

        protected abstract string GetProperty(string propName, int propSize = 32);
    }
}
