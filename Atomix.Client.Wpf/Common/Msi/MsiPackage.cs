using System;
using System.IO;
using System.Text;

namespace Atomix.Client.Wpf.Common.Msi
{
    class MsiPackage : PropsCollection
    {
        public string Path { get; private set; }

        public string UpgradeCode => this["UpgradeCode"];
        public string ProductCode => this["ProductCode"];
        public string ProductName => this["ProductName"];
        public string ProductVersion => this["ProductVersion"];
        public string ProductLanguage => this["ProductLanguage"];
        public string Manufacturer => this["Manufacturer"];

        public MsiPackage(string path)
        {
            if (!File.Exists(path))
                throw new Exception("Package not found");

            if (!MsiApi.VerifyPackage(path))
                throw new Exception("Package is invalid");

            Path = path;
        }

        protected override string GetProperty(string propName, int propSize = 32)
        {
            // Open Database
            var dbResult = MsiApi.MsiOpenDatabase(Path, MsiApi.MsiDatabasePersist.Readonly, out IntPtr db);
            if (dbResult != MsiApi.OpenDatabaseError.Success)
            {
                throw new Exception($"Failed to open package database: {Enum.GetName(dbResult.GetType(), dbResult)}");
            }

            // Open View
            var viewResult = MsiApi.MsiDatabaseOpenView(db, $"SELECT Value FROM Property WHERE Property='{propName}'", out IntPtr view);
            if (viewResult != MsiApi.DatabaseOpenViewError.Success)
            {
                MsiApi.MsiCloseHandle(db);
                throw new Exception($"Failed to open database view: {Enum.GetName(viewResult.GetType(), viewResult)}");
            }

            // Execute View
            var execResult = MsiApi.MsiViewExecute(view, IntPtr.Zero);
            if (execResult != MsiApi.ViewExecuteError.Success)
            {
                MsiApi.MsiViewClose(view);
                MsiApi.MsiCloseHandle(db);
                throw new Exception($"Failed to execute view: {Enum.GetName(execResult.GetType(), execResult)}");
            }

            // Fetch Record
            var recordResult = MsiApi.MsiViewFetch(view, out IntPtr record);
            if (recordResult != MsiApi.ViewFetchError.Success)
            {
                MsiApi.MsiViewClose(view);
                MsiApi.MsiCloseHandle(db);
                throw new Exception($"Failed to fetch a record: {Enum.GetName(recordResult.GetType(), recordResult)}");
            }

            // Get Value
            var value = GetRecordValue(record, 1);

            var r3 = MsiApi.MsiCloseHandle(db);
            var r2 = MsiApi.MsiCloseHandle(view);
            var r1 = MsiApi.MsiCloseHandle(record);

            return value;
        }

        private string GetRecordValue(IntPtr record, int field, int valueSize = 32)
        {
            var buffer = new StringBuilder(valueSize);
            var result = MsiApi.MsiRecordGetString(record, field, buffer, ref valueSize);

            if (result == MsiApi.RecordGetStringError.MoreData)
                return GetRecordValue(record, field, valueSize + 1);

            if (result != MsiApi.RecordGetStringError.Success)
                return null;

            return buffer.ToString();
        }
    }
}
