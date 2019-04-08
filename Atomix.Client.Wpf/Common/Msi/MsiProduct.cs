using System;
using System.Text;

namespace Atomix.Client.Wpf.Common.Msi
{
    /// <summary>
    /// Contains information about the installed product
    /// </summary>
    class MsiProduct : PropsCollection
    {
        public Guid ProductCode { get; private set; }

        public string Name => this["ProductName"];
        public string Version => this["VersionString"];
        public string Language => this["Language"];
        public string Manufacturer => this["Publisher"];
        public string PackageCode => this["PackageCode"];
        public string PackagePath => this["LocalPackage"];

        public MsiProduct(Guid productCode)
        {
            ProductCode = productCode;
        }

        protected override string GetProperty(string propName, int propSize = 32)
        {
            var buffer = new StringBuilder(propSize);
            var result = MsiApi.MsiGetProductInfo($"{{{ProductCode}}}", propName, buffer, ref propSize);

            if (result == MsiApi.GetProductInfoError.MoreData)
                return GetProperty(propName, propSize + 1);

            if (result != MsiApi.GetProductInfoError.Success)
                return null;

            return buffer.ToString();
        }
    }
}
