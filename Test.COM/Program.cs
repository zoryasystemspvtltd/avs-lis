using LIS.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Test.COM
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(DxI800ErrorHandler);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Home());
        }

        static void DxI800ErrorHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Logger.LogInstance.LogException(e);
        }
    }
}
