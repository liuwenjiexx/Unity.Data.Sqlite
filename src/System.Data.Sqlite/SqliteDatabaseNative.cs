using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.Sqlite
{


    public  partial class SqliteDatabase
    {
        private const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;
        private const string DLL_NAME = "sqlite3";


        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        internal static extern IntPtr sqlite3_backup_init(IntPtr dest, string destName, IntPtr source, string sourceName);


        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        internal static extern int sqlite3_backup_step(IntPtr p, int nPage);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        internal static extern int sqlite3_backup_finish(IntPtr p);


        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        internal static extern int sqlite3_backup_remaining(IntPtr p);

        [DllImport(DLL_NAME, CallingConvention = CALLING_CONVENTION)]
        internal static extern int sqlite3_backup_pagecount(IntPtr p);

    }


}
