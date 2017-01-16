
namespace ProcessKiller
{
    using System;
    using System.Windows.Forms;
    using System.Security.Principal;
    using Properties;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using GoogleAnalyticsTracker.Mvc4;


    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //KeySettings.Default.Reset();

            // check admin privilege
            if (IsAdministrator())
            {
                AppTracker.TrackPageView("Application", "Start");
                Application.Run(new MainForm());
            }
            else
            {
                ShowAdminPrivilegeMessageBox();
            }
        }



        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                    .IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void ShowAdminPrivilegeMessageBox()
        {
            MessageBox.Show(Resources.Program_ShowAdminErrorDialogBox_AdminPrivilegeMessage, 
                Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
