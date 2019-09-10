using System;
using System.Diagnostics;

using Atomex.Updates;
using Atomex.Updates.Abstract;
using Atomex.Client.Wpf.Common.Msi;

namespace Atomex.Client.Wpf.Common
{
    class MsiProductProvider : IProductProvider
    {
        public string Extension => ".msi";
        public Guid UpgradeCode { get; }

        public MsiProductProvider(string upgradeCode)
        {
            if (upgradeCode == null)
                throw new ArgumentNullException();

            if (!Guid.TryParse(upgradeCode, out var guid))
                throw new ArgumentException("Invalid UpgradeCode guid");

            UpgradeCode = guid;
        }

        public Version GetInstalledVersion()
        {
            var products = MsiApi.GetRelatedProducts(UpgradeCode);
            if (products.Count == 0)
            {
                // is it possible and should we worry?
                return new Version("0.0.0");
            }
            // can the products count be > 1 and how should we get the right one?
            else if (!Version.TryParse(products[0].Version, out var version))
            {
                // is it possible and what should we do?
                return new Version("0.0.0");
            }
            else
            {
                return version;
            }
        }

        public bool VerifyPackage(string packagePath)
        {
            if (!MsiApi.VerifyPackage(packagePath))
                return false;

            var package = new MsiPackage(packagePath);
            if (!Guid.TryParse(package.UpgradeCode, out var upgradeCode))
            {
                // is it possible and what should we do?
                return false;
            }

            if (upgradeCode != UpgradeCode)
                return false;

            // TODO: implement verification by signature 
            return true;
        }

        public bool VerifyPackageVersion(string packagePath, Version version)
        {
            if (!Version.TryParse(new MsiPackage(packagePath).ProductVersion, out var packageVersion))
            {
                // is it possible and what should we do?
                return false;
            }

            return packageVersion == version;
        }

        public void RunInstallation(string packagePath)
        {
            Process.Start(new ProcessStartInfo
            {
                // wait for 2 sec, kill the process and run the installer
                Arguments = $"/C ping 127.0.0.1 -n 2 > nul & taskkill /F /IM Atomex.Client.Wpf.exe & msiexec /i {packagePath} /q",
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = "cmd",
            });
        }
    }

    public static class MsiPackageManagerExt
    {
        public static Updater UseMsiProductProvider(this Updater updater, string upgradeCode)
        {
            return updater.UseProductProvider(new MsiProductProvider(upgradeCode));
        }
    }
}
