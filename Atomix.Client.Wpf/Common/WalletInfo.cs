using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atomix.Client.Wpf.Common
{
    public class WalletInfo
    {
        public const string DefaultWalletsDirectory = "wallets";
        public const string DefaultWalletFileName = "Atomix.wallet";

        public string Name { get; set; }
        public string Path { get; set; }

        public static IEnumerable<WalletInfo> AvailableWallets()
        {
            var result = new List<WalletInfo>();

            if (!Directory.Exists(DefaultWalletsDirectory))
                return result;

            var walletsDirectory = new DirectoryInfo(
                $"{AppDomain.CurrentDomain.BaseDirectory}{DefaultWalletsDirectory}");

            result.AddRange(
                from directory in walletsDirectory.GetDirectories()
                let walletFile = directory.GetFiles(DefaultWalletFileName).FirstOrDefault()
                where walletFile != null
                select new WalletInfo
                {
                    Name = directory.Name,
                    Path = walletFile.FullName
                });

            return result;
        }

        public static string CurrentWalletDirectory =>
            $"{AppDomain.CurrentDomain.BaseDirectory}{DefaultWalletsDirectory}";
    }
}