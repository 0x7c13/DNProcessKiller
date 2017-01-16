
namespace ProcessKiller
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using ProcessKiller.Properties;

    public partial class SettingsForm : Form
    {
        private readonly KeyboardInputEventHelper _keyboardEventHook;
        private bool _listeningKeyboardEvent;
        private string _oldProcessKillerKey;
        private string _oldCountDownKey;

        private Keys _userDefinedProcessKillerKey, _userDefinedCountDownKey;

        public SettingsForm()
        {
            InitializeComponent();

            InitializeSettings();

            this.MouseDown += SettingsForm_MouseDown;
            this.KeySettingsGroupBox.MouseDown += SettingsForm_MouseDown;
            this.SettingsTable.MouseDown += SettingsForm_MouseDown;
            this.ProcessKillerButtonLabel.MouseDown += SettingsForm_MouseDown;
            this.CountDownButtonLabel.MouseDown += SettingsForm_MouseDown;

            _keyboardEventHook = new KeyboardInputEventHelper();
            _keyboardEventHook.KeyBoardKeyDownEvent += keyboard_key_down;

            GC.KeepAlive(_keyboardEventHook);

            this.Load += settings_form_load;
        }

        private void settings_form_load(object sender, EventArgs e)
        {
            if (Owner != null)
                Location = new Point(Owner.Location.X + Owner.Width / 2 - Width / 2,
                    Owner.Location.Y + Owner.Height / 2 - Height / 2);
        }

        private void SettingsForm_MouseDown(object sender, MouseEventArgs e)
        {
            this.ApplyButton.Select();
        }

        private void InitializeSettings()
        {
            this.ProcessKillerKeyTextbox.GotFocus += ProcessKillerKeyTextbox_GotFocus;
            this.ProcessKillerKeyTextbox.LostFocus += ProcessKillerKeyTextbox_OnDeFocus;
            this.CountDownKeyTextBox.GotFocus += CountDownKeyTextBox_GotFocus;
            this.CountDownKeyTextBox.LostFocus += CountDownKeyTextBox_OnDeFocus;

            Enum.TryParse(KeySettings.Default.ProcessKillerKey.ToString(), out _userDefinedProcessKillerKey);
            Enum.TryParse(KeySettings.Default.CountDownKey.ToString(), out _userDefinedCountDownKey);
            var gameDicPath = KeySettings.Default.GameDicPath;
            var serverName = KeySettings.Default.ServerName;

            this.ProcessKillerKeyTextbox.Text = _userDefinedProcessKillerKey.ToString();
            this.CountDownKeyTextBox.Text = _userDefinedCountDownKey.ToString();

            if (!string.IsNullOrEmpty(gameDicPath))
            {
                this.GameDicPathTextBox.Text = gameDicPath;
            }

            var serverList = DNServerInfo.GetServerListFromResource();

            this.ServerSelectionBox.Items.AddRange(serverList.Select(s => s.Name).ToArray());
            if (!string.IsNullOrEmpty(serverName))
            {
                this.ServerSelectionBox.SelectedItem = serverName;
            }

            this.CancelButton.Select();
        }

        private void ProcessKillerKeyTextbox_GotFocus(object sender, EventArgs e)
        {
            _listeningKeyboardEvent = true;
            _oldProcessKillerKey = this.ProcessKillerKeyTextbox.Text;
            this.ProcessKillerKeyTextbox.Text = string.Empty;
        }

        private void ProcessKillerKeyTextbox_OnDeFocus(object sender, EventArgs e)
        {
            _listeningKeyboardEvent = false;
            if (string.IsNullOrEmpty(this.ProcessKillerKeyTextbox.Text))
            {
                this.ProcessKillerKeyTextbox.Text = _oldProcessKillerKey;
            }
        }

        private void CountDownKeyTextBox_GotFocus(object sender, EventArgs e)
        {
            _listeningKeyboardEvent = true;
            _oldCountDownKey = this.CountDownKeyTextBox.Text;
            this.CountDownKeyTextBox.Text = string.Empty;
        }

        private void CountDownKeyTextBox_OnDeFocus(object sender, EventArgs e)
        {
            _listeningKeyboardEvent = false;
            if (string.IsNullOrEmpty(this.CountDownKeyTextBox.Text))
            {
                this.CountDownKeyTextBox.Text = _oldCountDownKey;
            }
        }

        private void keyboard_key_down(object sender, KeyEventArgs e)
        {
            if (!_listeningKeyboardEvent) return;

            if (IsDisposed || !this.IsHandleCreated) return;

            Console.WriteLine(e.KeyCode);

            if (this.ProcessKillerKeyTextbox.Focused)
            {
                _userDefinedProcessKillerKey = e.KeyCode;
                this.ProcessKillerKeyTextbox.Text = e.KeyCode.ToString();
            }
            else if (this.CountDownKeyTextBox.Focused)
            {
                _userDefinedCountDownKey = e.KeyCode;
                this.CountDownKeyTextBox.Text = e.KeyCode.ToString();
            }
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            SaveUserSettings();
           _keyboardEventHook.Dispose();
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            _keyboardEventHook.Dispose();
            this.Close();
        }

        private void PathSelectionButton_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = Resources.SettingsForm_PathSelection_Description
            };
            var result = folderBrowserDialog.ShowDialog();

            if (result != DialogResult.OK || string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath)) return;

            if (File.Exists(Path.Combine(folderBrowserDialog.SelectedPath, Resources.DragonNest_LauncherExe_Name + ".exe")) &&
                File.Exists(Path.Combine(folderBrowserDialog.SelectedPath, Resources.DragonNest_Exe_Name + ".exe")))
            {
                this.GameDicPathTextBox.Text = folderBrowserDialog.SelectedPath;
            }
            else
            {
                MessageBox.Show(Resources.SettingsForm_PathSelection_InvalidPath_Message, Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
            
        private void SaveUserSettings()
        {
            if (!string.IsNullOrEmpty(this.ProcessKillerKeyTextbox.Text))
            {
                KeySettings.Default.ProcessKillerKey= (int)_userDefinedProcessKillerKey;
            }

            if (!string.IsNullOrEmpty(this.CountDownKeyTextBox.Text))
            {
                KeySettings.Default.CountDownKey = (int)_userDefinedCountDownKey;
            }

            if (!string.IsNullOrEmpty(this.GameDicPathTextBox.Text))
            {
                KeySettings.Default.GameDicPath = this.GameDicPathTextBox.Text;
            }

            if (this.ServerSelectionBox.SelectedItem != null)
            {
                if (!string.Equals(this.ServerSelectionBox.Text, KeySettings.Default.ServerName, StringComparison.CurrentCultureIgnoreCase))
                {
                    AppTracker.TrackEvent("Settings", $"SetServer_{this.ServerSelectionBox.Text}");
                }
                KeySettings.Default.ServerName = this.ServerSelectionBox.SelectedItem.ToString();
            }

            KeySettings.Default.Save(); // Saves settings in application configuration file
        }
    }
}
