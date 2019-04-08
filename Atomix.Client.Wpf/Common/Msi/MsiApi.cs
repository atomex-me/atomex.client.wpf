using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Atomix.Client.Wpf.Common.Msi
{
    static class MsiApi
    {
        #region dll import

        #region MsiEnumProducts
        /// <summary>
        /// Enumerate the registered products, either installed or advertised
        /// </summary>
        /// <param name="iProductIndex">Zero-based index into registered products</param>
        /// <param name="lpProductBuf">Buffer of char count: 39 (size of string GUID)</param>
        /// <returns></returns>
        [DllImport("Msi.dll", CharSet = CharSet.Unicode)]
        internal static extern EnumProductsError MsiEnumProducts(
            int iProductIndex,
            StringBuilder lpProductBuf
        );

        internal enum EnumProductsError
        {
            Success = 0,
            NotEnoughMemory = 8,
            InvalidParameter = 87,
            NoMoreItems = 259,
            BadConfiguration = 1610
        }
        #endregion

        #region MsiEnumRelatedProducts
        /// <summary>
        /// Enumerate products with given upgrade code
        /// </summary>
        /// <param name="lpUpgradeCode">Upgrade code of products to enumerate</param>
        /// <param name="dwReserved">Reserved, must be 0</param>
        /// <param name="iProductIndex">Zero-based index into registered products</param>
        /// <param name="lpProductBuf">buffer of char count: 39 (size of string GUID)</param>
        /// <returns></returns>
        [DllImport("Msi.dll", CharSet = CharSet.Unicode)]
        internal static extern EnumRelatedProductsError MsiEnumRelatedProducts(
            string lpUpgradeCode,
            int dwReserved,
            int iProductIndex,
            StringBuilder lpProductBuf
        );

        internal enum EnumRelatedProductsError
        {
            Success = 0,
            NotEnoughMemory = 8,
            InvalidParameter = 87,
            NoMoreItems = 259,
            BadConfiguration = 1610
        }
        #endregion

        #region MsiGetProductInfo
        /// <summary>
        /// Return product info
        /// </summary>
        /// <param name="szProduct">Product code</param>
        /// <param name="szAttribute">Attribute name, case-sensitive</param>
        /// <param name="lpProductBuf">Returned value, NULL if not desired</param>
        /// <param name="pcchValueBuf">In/out buffer character count</param>
        /// <returns></returns>
        [DllImport("Msi.dll", CharSet = CharSet.Unicode)]
        internal static extern GetProductInfoError MsiGetProductInfo(
            string szProduct,
            string szAttribute,
            StringBuilder lpProductBuf,
            ref int pcchValueBuf
        );

        internal enum GetProductInfoError
        {
            Success = 0,
            InvalidParameter = 87,
            MoreData = 234,
            UnknownProduct = 1605,
            UnknownProperty = 1608,
            BadConfiguration = 1610
        }
        #endregion

        #region MsiVerifyPackage
        /// <summary>
        /// Determine whether a file is a package
        /// </summary>
        /// <param name="szPackagePath">Location of package</param>
        /// <returns></returns>
        [DllImport("Msi.dll", CharSet = CharSet.Unicode)]
        internal static extern VerifyPackageError MsiVerifyPackage(
            string szPackagePath
        );

        internal enum VerifyPackageError
        {
            Success = 0,
            InvalidParameter = 87,
            PackageOpenFailed = 1619,
            PackageInvalid = 1620
        }
        #endregion

        #region MsiOpenDatabase
        /// <summary>
        /// Open an installer database, specifying the persistance mode, which is a pointer.
        /// Predefined persist values are reserved pointer values, requiring pointer arithmetic.
        /// Execution of this function sets the error record, accessible via MsiGetLastErrorRecord.
        /// </summary>
        /// <param name="szDatabasePath">Path to database, 0 to create temporary database</param>
        /// <param name="phPersist">Output database path or one of predefined values</param>
        /// <param name="phDatabase">Location to return database handle</param>
        /// <returns></returns>
        [DllImport("msi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern OpenDatabaseError MsiOpenDatabase(
            string szDatabasePath,
            IntPtr phPersist,
            out IntPtr phDatabase
        );

        /// <summary>
        /// MsiOpenDatabase persist predefine values
        /// </summary>
        internal static class MsiDatabasePersist
        {
            /// <summary>
            /// Database open read-only, no persistent changes
            /// </summary>
            public static IntPtr Readonly => (IntPtr)0;

            /// <summary>
            /// Database read/write in transaction mode
            /// </summary>
            public static IntPtr Transact => (IntPtr)1;

            /// <summary>
            /// Database direct read/write without transaction
            /// </summary>
            public static IntPtr Direct => (IntPtr)2;

            /// <summary>
            /// Create new database, transact mode read/write
            /// </summary>
            public static IntPtr Create => (IntPtr)3;

            /// <summary>
            /// Create new database, direct mode read/write
            /// </summary>
            public static IntPtr CreateDirect => (IntPtr)4;

            /// <summary>
            /// Add flag to indicate patch file
            /// </summary>
            public static IntPtr PatchFile => throw new NotImplementedException();
        }

        internal enum OpenDatabaseError
        {
            Success = 0
        }
        #endregion

        #region MsiDatabaseOpenView
        /// <summary>
        /// Prepare a database query, creating a view object
        /// Returns ERROR_SUCCESS if successful, and the view handle is returned,
        /// else ERROR_INVALID_HANDLE, ERROR_INVALID_HANDLE_STATE, ERROR_BAD_QUERY_SYNTAX, ERROR_GEN_FAILURE
        /// Execution of this function sets the error record, accessible via MsiGetLastErrorRecord
        /// </summary>
        /// <param name="hDatabase">Database handle</param>
        /// <param name="szQuery">SQL query to be prepared</param>
        /// <param name="phView">returned view if SUCCESS</param>
        /// <returns></returns>
        [DllImport("msi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern DatabaseOpenViewError MsiDatabaseOpenView(
            IntPtr hDatabase,
            [MarshalAs(UnmanagedType.LPWStr)] string szQuery,
            out IntPtr phView
        );

        internal enum DatabaseOpenViewError
        {
            Success = 0,
            BadQuerySyntax = 1615,
            InvalidHandle = 6,
            InvalidHandleState = 1609,
            GenFailure = 31,
        }
        #endregion

        #region MsiViewExecute
        /// <summary>
        /// Exectute the view query, supplying parameters as required
        /// Returns ERROR_SUCCESS, ERROR_INVALID_HANDLE, ERROR_INVALID_HANDLE_STATE, ERROR_GEN_FAILURE
        /// Execution of this function sets the error record, accessible via MsiGetLastErrorRecord
        /// </summary>
        /// <param name="hView">View handle</param>
        /// <param name="hRecord">Optional parameter record, or 0 if none</param>
        /// <returns></returns>
        [DllImport("msi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ViewExecuteError MsiViewExecute(
            IntPtr hView,
            IntPtr hRecord
        );

        internal enum ViewExecuteError
        {
            Success = 0,
            InvalidHandle = 6,
            InvalidHandleState = 1609,
            GenFailure = 31,
        }
        #endregion

        #region MsiViewFetch
        /// <summary>
        /// Fetch the next sequential record from the view
        /// Result is ERROR_SUCCESS if a row is found, and its handle is returned
        /// else ERROR_NO_MORE_ITEMS if no records remain, and a null handle is returned
        /// else result is error: ERROR_INVALID_HANDLE_STATE, ERROR_INVALID_HANDLE, ERROR_GEN_FAILURE
        /// </summary>
        /// <param name="hView">View handle</param>
        /// <param name="hRecord">Returned data record if fetch succeeds</param>
        /// <returns></returns>
        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern ViewFetchError MsiViewFetch(
            IntPtr hView,
            out IntPtr hRecord
        );

        internal enum ViewFetchError
        {
            Success = 0,
            NoMoreItems = 259,
            InvalidHandle = 6,
            InvalidHandleState = 1609,
            GenFailure = 31,
        }
        #endregion

        #region MsiRecordGetString
        /// <summary>
        /// Return the string value of a record field
        /// Integer fields will be converted to a string
        /// Null and non-existent fields will report a value of 0
        /// Fields containing stream data will return ERROR_INVALID_DATATYPE
        /// Returns ERROR_SUCCESS, ERROR_MORE_DATA, ERROR_INVALID_HANDLE, ERROR_INVALID_FIELD, ERROR_BAD_ARGUMENTS
        /// </summary>
        /// <param name="hRecord">Record handle</param>
        /// <param name="iField">Index of the field started from 1</param>
        /// <param name="szValueBuf">Buffer for returned value</param>
        /// <param name="pcchValueBuf">In/out buffer character count</param>
        /// <returns></returns>
        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern RecordGetStringError MsiRecordGetString(
            IntPtr hRecord,
            int iField,
            StringBuilder szValueBuf,
            ref int pcchValueBuf
        );

        internal enum RecordGetStringError
        {
            Success = 0,
            MoreData = 234,
            InvalidHandle = 6,
            InvalidDataType = 1804,
            InvalidField = 1616,
            BadArguments = 160
        }
        #endregion

        #region MsiViewClose
        /// <summary>
        /// Release the result set for an executed view, to allow re-execution
        /// Only needs to be called if not all records have been fetched
        /// Returns ERROR_SUCCESS, ERROR_INVALID_HANDLE, ERROR_INVALID_HANDLE_STATE
        /// </summary>
        /// <param name="hView">View handle</param>
        /// <returns></returns>
        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        internal static extern ViewCloseError MsiViewClose(
            IntPtr hView
        );

        internal enum ViewCloseError
        {
            Success = 0,
            InvalidHandle = 6,
            InvalidHandleState = 1609,
        }
        #endregion

        #region MsiCloseHandle
        /// <summary>
        /// Close a open handle of any type
        /// All handles obtained from API calls must be closed when no longer needed
        /// </summary>
        /// <param name="hAny">Msi handle to close</param>
        /// <returns></returns>
        [DllImport("Msi.dll", CharSet = CharSet.Unicode)]
        internal static extern CloseHandleError MsiCloseHandle(
            IntPtr hAny
        );

        internal enum CloseHandleError
        {
            Success = 0,
            InvalidHandle = 6
        }
        #endregion

        #endregion

        /// <summary>
        /// Returns a list of installed products
        /// </summary>
        /// <returns></returns>
        public static List<MsiProduct> GetAllProducts()
        {
            var products = new List<MsiProduct>();
            var productCode = new StringBuilder(39);

            while (true)
            {
                var result = MsiEnumProducts(products.Count, productCode);

                if (result == EnumProductsError.NoMoreItems)
                    break;

                if (result != EnumProductsError.Success)
                    throw new Exception(Enum.GetName(result.GetType(), result));

                products.Add(new MsiProduct(Guid.Parse(productCode.ToString())));
            }

            return products;
        }

        /// <summary>
        /// Returns a list of installed products associated with the specified upgrade code
        /// </summary>
        /// <param name="upgradeCode">Upgrade code identifying a group of related products</param>
        /// <returns></returns>
        public static List<MsiProduct> GetRelatedProducts(Guid upgradeCode)
        {
            var products = new List<MsiProduct>();
            var productCode = new StringBuilder(39);

            while (true)
            {
                var result = MsiEnumRelatedProducts($"{{{upgradeCode}}}", 0, products.Count, productCode);

                if (result == EnumRelatedProductsError.NoMoreItems)
                    break;

                if (result != EnumRelatedProductsError.Success)
                    throw new Exception(Enum.GetName(result.GetType(), result));

                products.Add(new MsiProduct(Guid.Parse(productCode.ToString())));
            }

            return products;
        }

        /// <summary>
        /// Checks if a file is a valid installation package or not
        /// </summary>
        /// <param name="path">Path to the verifying file</param>
        /// <returns></returns>
        public static bool VerifyPackage(string path)
        {
            return MsiVerifyPackage(path) == VerifyPackageError.Success;
        }
    }
}
