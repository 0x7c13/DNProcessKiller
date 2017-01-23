
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
        private bool _disableProcessKiller;

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
            this.TimerCountDownTimeTextBox.KeyPress += TimerCountDownTimeTextBox_KeyPress;
            this.TimerCountDownWarnningTimeTextBox.KeyPress += TimerCountDownWarnningTimeTextBox_KeyPress;
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
            _disableProcessKiller = KeySettings.Default.DisableProcessKiller;
            var timerCountDownTime = KeySettings.Default.TimerCountDownTime;
            var timerCountDownWarnningTime = KeySettings.Default.TimerCountDownWarnningTime;
            var gameDicPath = KeySettings.Default.GameDicPath;
            var serverName = KeySettings.Default.ServerName;

            this.ProcessKillerKeyTextbox.Text = _userDefinedProcessKillerKey.ToString();
            this.CountDownKeyTextBox.Text = _userDefinedCountDownKey.ToString();
            this.TimerCountDownTimeTextBox.Text = timerCountDownTime.ToString();
            this.TimerCountDownWarnningTimeTextBox.Text = timerCountDownWarnningTime.ToString();

            if (!string.IsNullOrEmpty(gameDicPath))
            {
                this.GameDicPathTextBox.Text = gameDicPath;
            }

            if (_disableProcessKiller)
            {
                this.DisableProcessKillCheckBox.Checked = _disableProcessKiller;
                this.ProcessKillerKeyTextbox.Enabled = !_disableProcessKiller;
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
            string invalidFieldWarnningMessage;
            if (_hasInvalidFields(out invalidFieldWarnningMessage))
            {
                MessageBox.Show(invalidFieldWarnningMessage, Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                SaveUserSettings();
                _keyboardEventHook.Dispose();
                this.Close();
            }
        }

        private bool _hasInvalidFields(out string invalidFieldWarnningMessage)
        {
            invalidFieldWarnningMessage = string.Empty;

            int countDownTime, countDownWarnningTime;

            // check count down time
            {
                countDownTime = int.Parse(this.TimerCountDownTimeTextBox.Text);
                if (countDownTime <= 0)
                {
                    invalidFieldWarnningMessage = Resources.SettingsForm_InvalidField_CountDownTime;
                    return true;
                }
            }
            // check count down warnning time
            {
                countDownWarnningTime = int.Parse(this.TimerCountDownWarnningTimeTextBox.Text);
                if (countDownWarnningTime <= 0)
                {
                    invalidFieldWarnningMessage = Resources.SettingsForm_InvalidField_CountDownTime;
                    return true;
                }
            }
            // count down time should be greater than warnning time
            if (countDownTime <= countDownWarnningTime)
            {
                invalidFieldWarnningMessage = Resources.SettingsForm_InvalidField_CountDownWarnningTime;
                return true;
            }

            return false;
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

        private void DisableProcessKillCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _disableProcessKiller = this.DisableProcessKillCheckBox.Checked;
            this.ProcessKillerKeyTextbox.Enabled = !_disableProcessKiller;
            if (!_disableProcessKiller)
            {
                MessageBox.Show(Resources.SettingsForm_DisableProcessKillCheckBox_CheckedChanged_Warnning, Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void TimerCountDownTimeTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TimerCountDownWarnningTimeTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
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

            KeySettings.Default.TimerCountDownTime = int.Parse(this.TimerCountDownTimeTextBox.Text);

            KeySettings.Default.TimerCountDownWarnningTime = int.Parse(this.TimerCountDownWarnningTimeTextBox.Text);

            KeySettings.Default.DisableProcessKiller = this.DisableProcessKillCheckBox.Checked;

            KeySettings.Default.Save(); // Saves settings in application configuration file
        }
    }
}
