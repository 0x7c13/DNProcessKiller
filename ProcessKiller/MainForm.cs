
namespace ProcessKiller
{
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Windows.Forms;
    using System;
    using System.Linq;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Text;
    using Properties;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Win32;
    using System.Threading;
    using System.Reflection;

    public partial class MainForm : Form
    {
        private readonly ProcessMonitor _processMonitor;
        private Button _startGameButton;
        private Label _copyrightLabel;
        private LinkLabel _versionInfoLabel, _newVersionDownloadLinkLabel;
        private TimerButton _timerButton;
        private readonly FlowLayoutPanel _container;
        private FlowLayoutPanel _buttonsContainer;
        private readonly WinEventHelper _winEvent;
        private readonly KeyboardInputEventHelper _keyboardEventHook;
        private readonly Stopwatch _runningTimer;

        private const int DefaultProcessButtonHeight = 120;
        private const int DefaultTimerButtonHeight = 80;
        private const int DefaultProcessKillerButtonHeight = 40;
        private const int DefaultCopyrightLabelHeight = 20;
        private const int DefaultVersionInfoLabelHeight = 25;
        private const int DefaultClientRectangleWidth = 250;
        private const int DefaultSettingsButtonHeight = 30;
        private const int DefaultStartGameButtonHeight = 50;
        private const int DefaultNewVersionDownloadLinkLabelHeight = 20;

        private readonly Color _buttonDisabledBackColor = Color.DimGray;

        private readonly Color _startGameButtonBackColor = ControlPaint.Light(ColorTranslator.FromHtml("#446CB3"), 0.8f);

        private readonly Color _processButtonBackColor = ControlPaint.Light(ColorTranslator.FromHtml("#89c4f4"), 0.5f);
        private readonly Color _processButtonHighlightedBackColor = ColorTranslator.FromHtml("#4b77b1");

        private readonly Color _processKillerBackColor = ControlPaint.Light(ColorTranslator.FromHtml("#f1a9a0"), 0.5f);
        private readonly Color _processKillerHighlightedBackColor = ColorTranslator.FromHtml("#e26a6a");

        private readonly Color _timerDefaultTextColor = Color.White;
        private readonly Color _timerAlertTextColor = Color.DarkRed;
        private readonly Color _timerDefaultBackColor = Color.DimGray;
        private readonly Color _timerRunningBackColor = ControlPaint.Dark(ColorTranslator.FromHtml("#87d37c"), 0.0f);

        private readonly FontFamily _defaultFontFamily;

        private Keys _processKillerKey, _countDownKey;
        private string _gameDicPath;
        private bool _disableProcessKiller;
        private int _countDownTime, _countDownWarnningTime;

        private static readonly object _locker = new object();
        private bool _listeningToKeyboardEvents = true;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;

        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x20000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        public MainForm()
        {
            _runningTimer = new Stopwatch();
            _runningTimer.Start();

            this.Text = Resources.ApplicationName;

            InitializeComponent();

            InitializeSettings();

            _defaultFontFamily = FontFamily.GenericSansSerif;

            try
            {
                _defaultFontFamily = new FontFamily("Microsoft YaHei"); // use Microsoft YaHei if it is available
            }
            catch (Exception)
            {
                // ignore
            }
        
            _processMonitor = new ProcessMonitor(Resources.DragonNest_Exe_Name);

            _processMonitor.OnEventArrived += event_arrived;

            InitializeTitleBar();

            _container = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = DefaultClientRectangleWidth,
                Padding = new Padding(0, this.TitleBar.Height, 0, 3),
            };

            this.Controls.Add(_container);

            InitializeControls(_container);

            _processMonitor.StartMonitoring();

            _winEvent = new WinEventHelper();
            _winEvent.OnWindowForegroundChanged += window_event_triggered;
            _winEvent.OnWindowMinimizeStart += window_event_triggered;
            _winEvent.OnWindowMinimizeEnd += window_event_triggered;

            _keyboardEventHook = new KeyboardInputEventHelper();
            _keyboardEventHook.KeyBoardKeyDownEvent += keyboard_key_down;

            this.ResizeEnd += resize_end;

            // hacky way to keep delegates alive
            GC.KeepAlive(_winEvent);
            GC.KeepAlive(_keyboardEventHook);

            // new version check
            Task.Run(() =>
            {
                NotifyIfNewVersionFound();
            });

#if DEBUG
            TitleBarLabel.Text += " DEBUG MODE ";
#endif
        }

        private void NotifyIfNewVersionFound()
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            try
            {
                var latestVersion = new DuowanAppVersionResolver(Resources.Application_Web_HomePage).GetLatestVersion();
                var currentVersion = Assembly.GetEntryAssembly().GetName().Version;

                if (currentVersion.CompareTo(latestVersion) < 0)
                {
                    if (IsDisposed || !this.IsHandleCreated) return;

                    this.BeginInvoke(new MethodInvoker(() =>
                    {
                        _newVersionDownloadLinkLabel.Text = Resources.MainForm_GetNewVersionDownloadLinkLabel_NewVersionFound_Message +
                        " " + latestVersion.ToString() + " " + Resources.MainForm_GetNewVersionDownloadLinkLabel_NewVersionFound_Message_End;
                        _container.Controls.Add(_newVersionDownloadLinkLabel);
                        ResizeWindowIfNeeded();
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }

        private void InitializeTitleBar()
        {
            this.TitleBarLabel.MouseDown += window_mouse_down;
        }

        private void window_mouse_down(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void InitializeSettings()
        {
            Enum.TryParse(KeySettings.Default.ProcessKillerKey.ToString(), out _processKillerKey);
            Enum.TryParse(KeySettings.Default.CountDownKey.ToString(), out _countDownKey);
            _countDownTime = KeySettings.Default.TimerCountDownTime;
            _countDownWarnningTime = KeySettings.Default.TimerCountDownWarnningTime;
            _disableProcessKiller = KeySettings.Default.DisableProcessKiller;
            _gameDicPath = _getGameDir();

            if (_timerButton != null)
            {
                _timerButton.CountDownTime = TimeSpan.FromSeconds(_countDownTime);
                _timerButton.AlertThreshold = TimeSpan.FromSeconds(_countDownWarnningTime);
                _timerButton.Reset();
            }
        }

        private Button GetStartGameButton()
        {
            var button = new NoFocusCueButton()
            {
                Width = DefaultClientRectangleWidth - (_buttonsContainer.Padding.Left + _buttonsContainer.Margin.Left + _container.Padding.Left + _container.Margin.Left) * 2,
                Height = DefaultStartGameButtonHeight,
                Font = new Font(_defaultFontFamily, 12, FontStyle.Regular),
                Text = Resources.MainForm_GetStartGameButton_title,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(6, 9, 6, 0),
                BackColor = _startGameButtonBackColor,
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += start_game_button_click;

            return button;
        }

        private Label GetCopyrightLabel(int width)
        {
            return new Label
            {
                Width = width,
                Height = DefaultCopyrightLabelHeight,
                Text = Resources.MainForm_InitializeCopyRightLabel_CopyrightMessage,
                Font = new Font(_defaultFontFamily, 9, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
            };
        }

        private LinkLabel GetVersionInfoLabel(int width)
        {
            var currentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
            return new LinkLabel()
            {
                Width = width,
                Height = DefaultVersionInfoLabelHeight,
                Text = Resources.MainForm_InitializeVersionLabel_Version + " " + currentVersion + " " + Resources.Application_PublishDate,
                Font = new Font(_defaultFontFamily, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                LinkColor = Color.White,
                ActiveLinkColor = Color.Gray,
                LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline,
            };
        }

        private LinkLabel GetNewVersionDownloadLinkLabel(int width)
        {
            return new LinkLabel()
            {
                Width = width,
                Height = DefaultNewVersionDownloadLinkLabelHeight,
                Text = Resources.MainForm_GetNewVersionDownloadLinkLabel_NewVersionFound_Message,
                Font = new Font(_defaultFontFamily, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                LinkColor = Color.White,
                BackColor = Color.DarkGreen,
                ActiveLinkColor = Color.Gray,
                LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline,
            };
        }

        private ProcessButton GetProcessButton(Process process)
        {
            var button = new ProcessButton(_processButtonHighlightedBackColor, _processButtonBackColor, _buttonDisabledBackColor, 
                Resources.MainForm_button_click_KillingProcessMessage,
                Resources.MainForm_GetProcessButton_ProcessButtonTitle + " (PID:" + process.Id + ")",
                Resources.MainForm_GetProcessButton_ProcessButtonHighlightedTitle + " (PID:" + process.Id + ")")
            {
                Width = DefaultClientRectangleWidth - (_buttonsContainer.Padding.Left + _buttonsContainer.Margin.Left + _container.Padding.Left + _container.Margin.Left) * 2,
                Height = DefaultProcessButtonHeight,
                Font = new Font(_defaultFontFamily, 12, FontStyle.Regular),
                Text = Resources.MainForm_GetProcessButton_ProcessButtonTitle + " (PID:" + process.Id + ")",
                Process = process,
                Margin = _disableProcessKiller ? new Padding(3, 3, 3, 3) : new Padding(3, 5, 3, 1),
                FlatStyle = FlatStyle.Flat,
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += process_button_click;
            
            return button;
        }

        private Button GetProcessKillerButton(ProcessButton processButton)
        {
            var button = new ProcessKillerButton(processButton, _processKillerHighlightedBackColor, _processKillerBackColor, _buttonDisabledBackColor,
                Resources.MainForm_InitializeButtons_ClickToKillProcessMessage,
                Resources.MainForm_InitializeButtons_ClickToKillProcessHighlightedMessage,
                Resources.MainForm_button_click_KillingProcessMessage
                )
            {
                Width =DefaultClientRectangleWidth - (_buttonsContainer.Padding.Left + _buttonsContainer.Margin.Left + _container.Padding.Left + _container.Margin.Left) * 2,
                Height = DefaultProcessKillerButtonHeight,
                Font = new Font(_defaultFontFamily, 11, FontStyle.Regular),
                Margin = new Padding(3, 0, 3, 5),
                Text = Resources.MainForm_InitializeButtons_ClickToKillProcessMessage,
                FlatStyle = FlatStyle.Flat,
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += process_killer_button_click;

            return button;
        }

        private TimerButton GetTimerButton(int width, TimeSpan countDown, TimeSpan alertThreshold)
        {
            var button = new TimerButton(countDown, alertThreshold, _timerDefaultTextColor, _timerAlertTextColor, _timerRunningBackColor, _timerDefaultBackColor)
            {
                Width = width,
                Height = DefaultTimerButtonHeight,
                Font = new Font(_defaultFontFamily, 32, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(6, 0, 6, 3),
                Text = countDown.TotalSeconds.ToString("0"),
                BackColor = _timerDefaultBackColor,
                ForeColor = _timerDefaultTextColor,
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += timer_button_click;

            return button;
        }

        private Button GetSettingsButton()
        {
            var button = new NoFocusCueButton()
            {
                Width = DefaultClientRectangleWidth - (_buttonsContainer.Padding.Left + _buttonsContainer.Margin.Left + _container.Padding.Left + _container.Margin.Left) * 2,
                Height = DefaultSettingsButtonHeight,
                Font = new Font(_defaultFontFamily, 10, FontStyle.Regular),
                Text = Resources.MainForm_GetSettingsButton_SettingsButtonTitle,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(6, 3, 6, 6),
                BackColor = Color.DarkGray,
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += settings_button_click;
            return button;
        }

        private void InitializeControls(FlowLayoutPanel container)
        {
            var width = container.Width - container.Margin.All * 2;

            _buttonsContainer = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };
            _buttonsContainer.ControlAdded += process_button_added;
            _buttonsContainer.ControlRemoved += process_button_removed;

            _startGameButton = GetStartGameButton();

            container.Controls.Add(_startGameButton);

            for (var i = 0; i < _processMonitor.GetRunningProcesses().Count; i++)
            {
                var process = _processMonitor.GetRunningProcesses()[i];
                var button = GetProcessButton(process);
                _buttonsContainer.Controls.Add(button);
                var killerButton = GetProcessKillerButton(button);
                if (_disableProcessKiller)
                {
                    killerButton.Hide();
                }
                _buttonsContainer.Controls.Add(killerButton);
                button.ShowPerformanceCounter();
            }
            _container.Controls.Add(_buttonsContainer);

            _timerButton = GetTimerButton(width - container.Margin.All * 2, 
                TimeSpan.FromSeconds(_countDownTime), TimeSpan.FromSeconds(_countDownWarnningTime));
            _container.Controls.Add(_timerButton);

            var settingsButton = GetSettingsButton();
            container.Controls.Add(settingsButton);
            _copyrightLabel = GetCopyrightLabel(width);
            _copyrightLabel.MouseDown += window_mouse_down;
            container.Controls.Add(_copyrightLabel);
            _versionInfoLabel = GetVersionInfoLabel(width);
            _versionInfoLabel.LinkClicked += home_link_clicked;
            container.Controls.Add(_versionInfoLabel);
            _newVersionDownloadLinkLabel = GetNewVersionDownloadLinkLabel(width);
            _newVersionDownloadLinkLabel.LinkClicked += new_version_link_clicked;

            ResizeWindowIfNeeded();
        }

        private void home_link_clicked(object sender, EventArgs e)
        {
            AppTracker.TrackEvent("MainForm", "AppHomePageLinkClicked");
            Process.Start(Resources.Application_Web_HomePage);
        }

        private void new_version_link_clicked(object sender, EventArgs e)
        {
            AppTracker.TrackEvent("MainForm", "NewVersionLinkClicked");
            Process.Start(Resources.Application_Web_HomePage);
        }

        private void SetProcessButtonActiveByProcessId(uint pid)
        {
            foreach (var processButton in _buttonsContainer.Controls.OfType<ProcessButton>())
            {
                if (processButton.Process.Id == pid)
                {
                    //Console.WriteLine($"Process [{pid}] becomes active");
                    processButton.Highlight();
                }
                else
                {
                    processButton.Unhighlight();
                }
            }

            foreach (var processKillerButton in _buttonsContainer.Controls.OfType<ProcessKillerButton>())
            {
                if (processKillerButton.ProcessButton.Process.Id == pid)
                {
                    processKillerButton.Highlight();
                }
                else
                {
                    processKillerButton.Unhighlight();
                }
            }
        }

        private void ResizeWindowIfNeeded()
        {
            var height = this.TitleBar.Height + 
                (from Control control in _container.Controls select control.Height).Sum() + 
                _container.Margin.All * (_container.Controls.Count + 2) 
                + _container.Margin.All * 2;

            if (DefaultClientRectangleWidth != this.ClientSize.Width || height != this.ClientSize.Height)
            {
                this.ClientSize = new Size(DefaultClientRectangleWidth, height);
            }
        }

        private void process_button_added(object sender, ControlEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated) return;
        }

        private void process_button_removed(object sender, ControlEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            if (_buttonsContainer.Controls.Count != 0)
            {
                var pid = WinEventHelper.GetForegroundWindowThreadProcessId();
                //Console.WriteLine(pid);
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    SetProcessButtonActiveByProcessId(pid);
                }));
            }
        }

        private void event_arrived(ProcessEventType type, uint pid)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            lock (_locker)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    if (type == ProcessEventType.Start)
                    {
                        var process = Process.GetProcessById((int)pid);
                        var button = GetProcessButton(process);
                        var killerButton = GetProcessKillerButton(button);
                        _buttonsContainer.Controls.Add(button);
                        if (_disableProcessKiller)
                        {
                            killerButton.Hide();
                        }
                        _buttonsContainer.Controls.Add(killerButton);
                        button.ShowPerformanceCounter();
                        SetProcessButtonActiveByProcessId(pid);
                    }
                    else if (type == ProcessEventType.Stop)
                    {
                        var processButtons = _buttonsContainer.Controls.OfType<ProcessButton>().ToList();
                        var processKillerButtons = _buttonsContainer.Controls.OfType<ProcessKillerButton>().ToList();

                        var buttonToRemove = processButtons.FirstOrDefault(button => button.Process.Id == pid);
                        if (buttonToRemove == null) return;

                        buttonToRemove.HidePerformanceCounter();
                        _buttonsContainer?.Controls.Remove(buttonToRemove);
                        buttonToRemove.Dispose();

                        var killerButtonToRemove = processKillerButtons.FirstOrDefault(button => button.ProcessButton.Process.Id == pid);
                        if (killerButtonToRemove == null) return;
                        _buttonsContainer?.Controls.Remove(killerButtonToRemove);
                        killerButtonToRemove.Dispose();
                    }

                    ResizeWindowIfNeeded();
                }));
            }
        }

        private void start_game_button_click(object sender, EventArgs e)
        {
            _startGameButton.Enabled = false;

            try
            {
                if (string.IsNullOrEmpty(_gameDicPath))
                {
                    throw new FileNotFoundException(Resources.DNExe_NotFound_Message);
                }

                string fileName = Resources.DragonNest_LauncherExe_Name;

                var instanceCount = _processMonitor.GetRunningProcesses().Count;

                if (instanceCount > 0)
                {
                    var instanceId = _getNextAvailableInstanceId(instanceCount);
                    fileName = $"{Resources.DragonNest_Exe_Name}-PK-{instanceId}";
                    File.Copy(Path.Combine(_gameDicPath, Resources.DragonNest_Exe_Name + ".exe"), Path.Combine(_gameDicPath, fileName +".exe"), overwrite: true);
                    _setCompatFlags(Path.Combine(_gameDicPath, fileName + ".exe"));
                }

                string handleExePath = Path.Combine(_gameDicPath, "handle.exe");

                var serverName = KeySettings.Default.ServerName;
                if (string.IsNullOrEmpty(serverName))
                {
                    throw new Exception(Resources.DNServer_NotFound_Message);
                }

                var serverList = DNServerInfo.GetServerListFromResource();
                var server = serverList.First(s => string.Equals(serverName, s.Name));

                string args = $"/ip:{server.IpAddress} /port:{server.Port} /Lver:2 /use_packing";

                var handleFile = Path.Combine(_gameDicPath, "handle.exe");
                _extractHandleExeToTempFolderIfNotFound(handleFile);

                var appLauncher = new MutexAppLauncher(handleExePath, Resources.DragonNest_Exe_Name);
                appLauncher.Launch(_gameDicPath, fileName, args);

                if (KeySettings.Default.IsFirstTimeClickOnStartButton)
                {
                    MessageBox.Show(Resources.MainForm_start_game_button_first_click_message, Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    KeySettings.Default.IsFirstTimeClickOnStartButton = false;
                    KeySettings.Default.Save();
                }
                _startGameButton.Text = Resources.MainForm_start_game_button_game_start_message;

                AppTracker.TrackEvent("AppLauncher", $"Launch_Instance_{instanceCount + 1}");
            }
            catch (Exception ex)
            {
                _startGameButton.Enabled = true;
                _startGameButton.Text = Resources.MainForm_GetStartGameButton_title;
                Console.WriteLine(ex);
                MessageBox.Show(ex.Message, Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            Task.Run(() =>
            {
                Thread.Sleep(20 * 1000);
                if (IsDisposed || !this.IsHandleCreated) return;

                this.BeginInvoke(new MethodInvoker(() =>
                {
                    _startGameButton.Text = Resources.MainForm_GetStartGameButton_title;
                    _startGameButton.Enabled = true;
                }));
            });
        }

        private int _getNextAvailableInstanceId(int instanceCount)
        {
            var id = instanceCount;
            while (true)
            {
                if (Process.GetProcessesByName($"{Resources.DragonNest_Exe_Name}-PK-{id}").Length == 0) return id;
                id++;
            }
        }

        private string _getGameDir()
        {
            if (!string.IsNullOrEmpty(KeySettings.Default.GameDicPath))
            {
                return KeySettings.Default.GameDicPath;
            }

            // try get the game dic from registry
            var regPath = Resources.DragonNest_Registry_Path;
            var key = Registry.LocalMachine.OpenSubKey(regPath);

            var gameDir = key?.GetValue("Path")?.ToString();

            if (string.IsNullOrEmpty(gameDir))
            {
                return string.Empty;
            }

            if (File.Exists(Path.Combine(gameDir, Resources.DragonNest_LauncherExe_Name + ".exe")) &&
                File.Exists(Path.Combine(gameDir, Resources.DragonNest_Exe_Name + ".exe")))
            {
                _gameDicPath = gameDir;
                KeySettings.Default.GameDicPath = gameDir;
                KeySettings.Default.Save();
                return gameDir;
            }
            else
            {
                return string.Empty;
            }
        }

        // Set compatibility flags
        // HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers
        private void _setCompatFlags(string file)
        {
            var softwareKey = Registry.CurrentUser.OpenSubKey("Software", true);
            var microsoftKey = softwareKey?.OpenSubKey("Microsoft", true);
            var winNtKey = microsoftKey?.OpenSubKey("Windows NT", true);
            var currentVersionKey = winNtKey?.OpenSubKey("CurrentVersion", true);
            var appCompactFlagsKey = currentVersionKey?.OpenSubKey("AppCompatFlags", true);
            var layersKey = appCompactFlagsKey?.OpenSubKey("Layers", true);

            layersKey?.SetValue(file, "~ RUNASADMIN HIGHDPIAWARE");
        }

        private void _extractHandleExeToTempFolderIfNotFound(string filePath)
        {
            if (!File.Exists(filePath))
            {
                using (Stream resource = new MemoryStream(Resources.handle))
                {
                    using (Stream output = File.OpenWrite(filePath))
                    {
                        resource.CopyTo(output);
                    }
                }
            }
        }

        private void process_button_click(object sender, EventArgs e)
        {
            try
            {
                var processButton = sender as ProcessButton;
                if (processButton == null) return;

                processButton.Highlight();
                WinEventHelper.BringProcessToFront(processButton.Process); 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void process_killer_button_click(object sender, EventArgs e)
        {
            try
            {
                var processKillerButton = sender as ProcessKillerButton;
                if (processKillerButton == null) return;

                processKillerButton.KillProcess();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void timer_button_click(object sender, EventArgs e)
        {
            var timerButton = sender as TimerButton;

            if (timerButton == null) return;

            if (!timerButton.Started())
            {
                timerButton.StartTimer();
            }
            else
            {
                timerButton.StopTimer();
            }
        }

        private void window_event_triggered(uint pid)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                pid = WinEventHelper.GetForegroundWindowThreadProcessId();
                SetProcessButtonActiveByProcessId(pid);
            }));
        }

        private void keyboard_key_down(object sender, KeyEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated || !_listeningToKeyboardEvents) return;

            //Console.WriteLine(e.KeyCode);

            if (e.KeyCode == _processKillerKey)
            {
                if (_disableProcessKiller) return;
                
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    if (_buttonsContainer.Controls.Count == 0) return;
                
                    var processButtons = _buttonsContainer.Controls.OfType<ProcessButton>().ToList();
                    var processKillerButtons = _buttonsContainer.Controls.OfType<ProcessKillerButton>().ToList();

                    var buttonToClick = processButtons.FirstOrDefault(processButton => processButton.IsHighlighted());
                    if (buttonToClick == null) return;
                    var killerButtonToClick = processKillerButtons.FirstOrDefault(processButton => processButton.ProcessButton.Process.Id == buttonToClick.Process.Id);
                    process_killer_button_click(killerButtonToClick, null);
                }));
            }
            else if (e.KeyCode == _countDownKey)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    if (!_timerButton.Started())
                    {
                        _timerButton.StartTimer();
                    }
                    else
                    {
                        _timerButton.StopTimer();
                    }
                }));
            }
        }

        private void settings_button_click(object sender, EventArgs e)
        {
            _listeningToKeyboardEvents = false;
            var settingsForm = new SettingsForm {StartPosition = FormStartPosition.CenterParent};
            settingsForm.Closed += settings_form_closed;
            settingsForm.ShowDialog(this);
        }

        private void exit_button_Click(object sender, EventArgs e)
        {
            _runningTimer.Stop();
            var runningHours = (int)Math.Round(_runningTimer.Elapsed.TotalHours);
            AppTracker.TrackEvent("Timer", $"AppRunningTime_{runningHours}");
            Application.Exit();
        }

        private void minimize_button_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Resources.MainForm_HelpButton_Message, Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Question);
        }

        private void settings_form_closed(object sender, EventArgs e)
        {
            InitializeSettings();
            var processKillerButtons = _buttonsContainer.Controls.OfType<ProcessKillerButton>().ToList();
            foreach (var processKillerButton in processKillerButtons)
            {
                if (_disableProcessKiller)
                {
                    processKillerButton.Hide();
                }
                else
                {
                    processKillerButton.Show();
                }
            }

            ResetProcessButtonMargin();

            ResizeWindowIfNeeded();
            _listeningToKeyboardEvents = true;
        }

        private void ResetProcessButtonMargin()
        {
            var processButtons = _buttonsContainer.Controls.OfType<ProcessButton>().ToList();
            foreach (var processButton in processButtons)
            {
                processButton.Margin = _disableProcessKiller ? new Padding(3, 3, 3, 3) : new Padding(3, 5, 3, 1);
            }
        }

        private void resize_end(object sender, EventArgs e)
        {
            ResizeWindowIfNeeded();
        }
    }
}
