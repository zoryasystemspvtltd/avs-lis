using LIS.Logger;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace LisTCPIPConsole
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (CheckDate())
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LisConsoleErrorHandler);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Home());
            }
        }

        static void LisConsoleErrorHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Logger.LogInstance.LogException(e);
        }

        /// <summary>
        /// If the folder is exist then we will check the application key means date.
        /// If folder not exist then application will runwithout checking.
        /// </summary>
        /// <returns></returns>
        private static bool CheckDate()
        {
            var folderPath = $"{Environment.CurrentDirectory}\\Check";
            if (Directory.Exists(folderPath))
            {             
                var dateString = File.ReadAllText(folderPath+ "\\ApplicationKey.key");
                DateTime date = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
                if (date > DateTime.Now)
                {
                    Logger.LogInstance.LogDebug("Application Key is alive!");
                    return true;
                }
                else
                {
                    Logger.LogInstance.LogDebug("Application Key has expired!");
                    return false;
                }
            }
            Logger.LogInstance.LogDebug("Application Key is alive!");
            return true;
        }
    }
}
