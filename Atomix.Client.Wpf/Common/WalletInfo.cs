﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atomix.Core;
using Serilog;

namespace Atomix.Client.Wpf.Common
{
    public class WalletInfo
    {
        private const string DefaultWalletsDirectory = "wallets";
        public const string DefaultWalletFileName = "atomix.wallet";

        public string Name { get; set; }
        public string Path { get; set; }
        public Network Network { get; set; }
        public string Description => Network == Network.MainNet
            ? Name
            : $"[test] {Name}";

        public static IEnumerable<WalletInfo> AvailableWallets()
        {
            var result = new List<WalletInfo>();

            if (!Directory.Exists(DefaultWalletsDirectory))
                return result;

            var walletsDirectory = new DirectoryInfo(
                $"{AppDomain.CurrentDomain.BaseDirectory}{DefaultWalletsDirectory}");

            foreach (var directory in walletsDirectory.GetDirectories())
            {
                var walletFile = directory
                    .GetFiles(DefaultWalletFileName)
                    .FirstOrDefault();

                if (walletFile != null)
                {
                    try
                    {
                        Network type;

                        using (var stream = walletFile.OpenRead())
                        {
                            type = stream.ReadByte() == 0
                                ? Network.MainNet
                                : Network.TestNet;
                        }

                        result.Add(new WalletInfo
                        {
                            Name = directory.Name,
                            Path = walletFile.FullName,
                            Network = type
                        });
                    }
                    catch (Exception)
                    {
                        Log.Warning("Wallet file {@fullName} scan error", walletFile.FullName);
                    }
                }
            }

            result.Sort((a, b) => a.Network.CompareTo(b.Network));

            return result;
        }

        public static string CurrentWalletDirectory =>
            $"{AppDomain.CurrentDomain.BaseDirectory}{DefaultWalletsDirectory}";
    }
}