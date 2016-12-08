
namespace ProcessKiller
{
    using System;
    using System.Windows.Forms;
    using System.Security.Principal;
    using Properties;

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

            // check admin privilege
            if (IsAdministrator())
            {
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
                Resources.Program_ShowAdminErrorDialogBox_ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
