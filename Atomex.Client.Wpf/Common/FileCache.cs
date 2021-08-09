using System;
using System.IO;
using System.Text;

using Serilog;

using Atomex.Common;
using Atomex.Cryptography;

namespace Atomex.Client.Wpf.Common
{
    public static class FileCache
    {
        public const int MaxCacheFileSize = 1024 * 1024; // 1 Mb

        public static string DefaultCacheFolder { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");

        public static byte[] Get(string name, TimeSpan cachePeriod)
        {
            var key = Hex.ToHexString(Sha256.Compute(Encoding.UTF8.GetBytes(name)));
            var fileExtension = "cache";
            var fileName = $"{key}.{fileExtension}";
            var filePath = Path.Combine(DefaultCacheFolder, fileName);

            try
            {
                if (!Directory.Exists(DefaultCacheFolder))
                    Directory.CreateDirectory(DefaultCacheFolder);

                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > MaxCacheFileSize)
                    return null;

                if (cachePeriod != TimeSpan.MaxValue && fileInfo.LastWriteTimeUtc + cachePeriod < DateTime.UtcNow)
                    return null;

                return File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                Log.Error(e, "FileCache.Get error.");
            }

            return null;
        }

        public static void Set(string name, byte[] data)
        {
            var key = Hex.ToHexString(Sha256.Compute(Encoding.UTF8.GetBytes(name)));
            var fileExtension = "cache";
            var fileName = $"{key}.{fileExtension}";
            var filePath = Path.Combine(DefaultCacheFolder, fileName);

            try
            {
                File.WriteAllBytes(filePath, data);
            }
            catch (Exception e)
            {
                Log.Error(e, "FileCache.Set error.");
            }
        }
    }
}