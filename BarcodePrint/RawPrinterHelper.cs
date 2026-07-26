using System;
using System.IO;
using System.Runtime.InteropServices;

namespace BarcodePrint
{
    public class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW",
            SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool OpenPrinter(
            string szPrinter,
            out IntPtr hPrinter,
            IntPtr pd);

        [DllImport("winspool.drv", SetLastError = true)]
        static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW",
            SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO di);

        [DllImport("winspool.drv", SetLastError = true)]
        static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        static extern bool WritePrinter(
            IntPtr hPrinter,
            IntPtr pBytes,
            int dwCount,
            out int dwWritten);

        public static bool SendBytesToPrinter(string printerName,string fileName, byte[] bytes)
        {
            IntPtr hPrinter;
            DOCINFO di = new DOCINFO();
            di.pDocName = fileName;
            di.pDataType = "RAW";

            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                Console.WriteLine("Cannot open printer.");
                return false;
            }

            try
            {
                if (!StartDocPrinter(hPrinter, 1, di))
                    return false;

                if (!StartPagePrinter(hPrinter))
                    return false;

                IntPtr pUnmanagedBytes = Marshal.AllocHGlobal(bytes.Length);

                Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                bool success = WritePrinter(
                    hPrinter,
                    pUnmanagedBytes,
                    bytes.Length,
                    out int written);

                Marshal.FreeHGlobal(pUnmanagedBytes);

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);

                return success && written == bytes.Length;
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }

        public static bool SendFileToPrinter(string printerName, string fileName, string prnFile)
        {
            byte[] bytes = File.ReadAllBytes(prnFile);
            return SendBytesToPrinter(printerName, fileName, bytes);
        }
    }

}
