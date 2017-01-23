
namespace ProcessKiller
{
    using System;
    using System.Windows.Forms;
    using System.Security.Principal;
    using Properties;
    using System.Reflection; 


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
                var currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
                AppTracker.TrackEvent("Application", $"Start_Version_{currentVersion}");
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
