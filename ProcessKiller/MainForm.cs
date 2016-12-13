
namespace ProcessKiller
{
    using System.Collections.Concurrent;
    using System.Windows.Forms;
    using System;
    using System.Linq;
    using System.Diagnostics;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using System.Text;
    using Properties;

    public partial class MainForm : Form
    {
        private readonly ProcessMonitor _processMonitor;
        private ConcurrentBag<ProcessButton> _killerButtons;
        private Label _processNotFoundLabel, _copyrightLabel, _versionInfoLabel;
        private FlowLayoutPanel _container;
        private FlowLayoutPanel _buttonsContainer;
        private readonly WinEventHookHelper _winEventHook;
        private readonly KeyboardInputEventHelper _keyboardEventHook;

        private static int _defaultProcessButtonHeight = 150;
        private static int _defaultProcessNotFoundLabelHeight = 90;
        private static int _defaultCopyrightLabelHeight = 20;
        private static int _defaultVersionInfoLabelHeight = 20;
        private static int _border = 5;
        private static int _defaultClientRectangleWidth = 250;
        private readonly Color _buttonDefaultBackColor = ColorTranslator.FromHtml("#ecf0f1");

        private static readonly object _locker = new object();

        public MainForm()
        {
            _killerButtons = new ConcurrentBag<ProcessButton>();
            InitializeComponent();

            _processMonitor = new ProcessMonitor("dragonnest");
            //_processMonitor = new ProcessMonitor("notepad");

            _processMonitor.OnEventArrived += event_arrived;
            _container = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false, // Vertical rather than horizontal scrolling
                Width = _defaultClientRectangleWidth - _border * 2,
            };

            this.Controls.Add(_container);

            InitializeControls(_container);

            _processMonitor.StartMonitoring();

            _winEventHook = new WinEventHookHelper();
            _winEventHook.ProcessEnterForeground += process_enter_foreground;

            _keyboardEventHook = new KeyboardInputEventHelper();
            _keyboardEventHook.KeyBoardKeyDownEvent += keyboard_key_down;

            // hacky way to keep delegates alive
            GC.KeepAlive(_winEventHook);
            GC.KeepAlive(_keyboardEventHook);
        }

        private Label GetProcessNotFoundLabel(int width)
        {
            return new Label
            {
                Width = width,
                Height = _defaultProcessNotFoundLabelHeight,
                Text = Resources.MainForm_InitializeDefaultTextLabel_DefaultTextLabelMessage,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
            };
        }

        private Label GetCopyrightLabel(int width)
        {
            return new Label
            {
                Width = width,
                Height = _defaultCopyrightLabelHeight,
                Text = Resources.MainForm_InitializeCopyRightLabel_CopyrightMessage,
                Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
            };
        }

        private Label GetVersionInfoLabel(int width)
        {
            return new Label
            {
                Width = width,
                Height = _defaultVersionInfoLabelHeight,
                Text = Resources.MainForm_InitializeVersionLabel_Version,
                Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
            };
        }

        private ProcessButton GetProcessButton(Process process)
        {
            var button = new ProcessButton
            {
                Width = _defaultClientRectangleWidth - _buttonsContainer.Padding.Left * 2 - _buttonsContainer.Margin.Left * 2 - _container.Padding.Left * 2 - _container.Margin.Left * 2,
                Height = _defaultProcessButtonHeight,
                Text = Resources.MainForm_InitializeButtons_ClickToKillProcessMessage + " (PID:" + process.Id + ")",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular),
                BackColor = _buttonDefaultBackColor,
                Process = process,
            };
            button.Click += button_click;
            
            return button;
        }

        private void ClearControls()
        {
            this.Controls.Clear();
            // dispose if any
            foreach (var processButton in _killerButtons)
            {
                processButton.Dispose();
            }
            _processNotFoundLabel?.Dispose();
            _versionInfoLabel?.Dispose();
            _copyrightLabel?.Dispose();
            _killerButtons = new ConcurrentBag<ProcessButton>();
        }

        private void InitializeControls(FlowLayoutPanel container)
        {
            var width = container.Width;

            _processNotFoundLabel = GetProcessNotFoundLabel(width);

            if (_processMonitor.GetRunningProcesses().Count > 0)
            {
                _processNotFoundLabel.Hide();
            }
            container.Controls.Add(_processNotFoundLabel);

            _buttonsContainer = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = width - _border * 2,
            };

            for (var i = 0; i < _processMonitor.GetRunningProcesses().Count; i++)
            {
                var process = _processMonitor.GetRunningProcesses()[i];
                var button = GetProcessButton(process);
                _buttonsContainer.Controls.Add(button);
            }
            _container.Controls.Add(_buttonsContainer);
            
            _copyrightLabel = GetCopyrightLabel(width);
            container.Controls.Add(_copyrightLabel);
            _versionInfoLabel = GetVersionInfoLabel(width);
            container.Controls.Add(_versionInfoLabel);
            
            ResizeWindow();
        }

        private void ResizeWindow()
        {
            var height = (from Control control in _container.Controls select control.Height).Sum();
            if (_buttonsContainer.Controls.Count > 0)
            {
                height -= _defaultProcessNotFoundLabelHeight;
            }
            this.ClientSize = new Size(_defaultClientRectangleWidth, height + _border * _container.Controls.Count);
        }

        /*
        private void InitializeControls_old()
        {
            if (_processMonitor.GetRunningProcesses().Count == 0)
            {
                ClearControls();
                this.ClientSize = new Size(_defaultClientRectangleWidth, Math.Max(_defaultClientRectangleHeight, _defaultClientRectangleHeight * _processMonitor.GetRunningProcesses().Count) + _defaultCopyrightLabelHeight + _defaultVersionLabelHeight + _buttonBorder);

                //InitializeDefaultTextLabel();
                //InitializeCopyRightLabel(_defaultClientRectangleHeight);
                InitializeVersionLabel(_defaultClientRectangleHeight + _defaultCopyrightLabelHeight);
                return;
            }

            var top = _buttonBorder;
            var left = _buttonBorder;

            var buttons = new ConcurrentBag<ProcessButton>();

            for (var i = 0; i < _processMonitor.GetRunningProcesses().Count; i++)
            {
                var process = _processMonitor.GetRunningProcesses()[i];

                var button = new ProcessButton
                {
                    Left = left,
                    Top = top,
                    Width = _defaultClientRectangleWidth - _buttonBorder * 2,
                    Height = _defaultClientRectangleHeight - _buttonBorder * 2,
                    Text = Resources.MainForm_InitializeButtons_ClickToKillProcessMessage + " (PID:" + process.Id + ")",
                    Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular),
                    BackColor = _buttonDefaultBackColor,
                    Process = process,
                };
                button.Click += button_click;

                buttons.Add(button);
                top += button.Height + _buttonBorder;

                button.ShowPerformanceCounter();
            }

            ClearControls();
            this.ClientSize = new Size(_defaultClientRectangleWidth, Math.Max(_defaultClientRectangleHeight, _defaultClientRectangleHeight * _processMonitor.GetRunningProcesses().Count) + _defaultCopyrightLabelHeight + _defaultVersionLabelHeight + _buttonBorder);

            _killerButtons = buttons;
            foreach (var processButton in _killerButtons)
            {
                this.Controls.Add(processButton);
            }

            //InitializeCopyRightLabel(top);
            InitializeVersionLabel(top + _defaultCopyrightLabelHeight);
        }
        */

        // Why am I so damn lazy?...
        private void event_arrived(ProcessEventType type, uint pid)
        {
            if (IsDisposed) return;

            lock (_locker)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    if (type == ProcessEventType.Start)
                    {
                        if (_buttonsContainer.Controls.Count == 0)
                        {
                            _processNotFoundLabel.Hide();
                        }
                        _buttonsContainer?.Controls.Add(GetProcessButton(Process.GetProcessById((int)pid)));
                        ResizeWindow();
                        SetProcessButtonActiveByProcessId(pid);
                    }
                    else if (type == ProcessEventType.Stop)
                    {
                        var buttonToRemove = _buttonsContainer.Controls.Cast<ProcessButton>().FirstOrDefault(button => button.Process.Id == pid);
                        if (buttonToRemove != null)
                        {
                            _buttonsContainer?.Controls.Remove(buttonToRemove);
                            if (_buttonsContainer.Controls.Count == 0)
                            {
                                _processNotFoundLabel.Show();
                            }
                            ResizeWindow();
                        }
                    }
                }));
            }
        }

        private void button_click(object sender, EventArgs e)
        {
            try
            {
                var processButton = sender as ProcessButton;
                if (processButton == null) return;

                processButton.Enabled = false;
                processButton.Text = Resources.MainForm_button_click_KillingProcessMessage;
                processButton.Process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void process_enter_foreground(uint pid)
        {
            if (IsDisposed) return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                SetProcessButtonActiveByProcessId(pid);
            }));
        }

        void keyboard_key_down(object sender, KeyEventArgs e)
        {
            if (IsDisposed) return;

            if (e.KeyCode == Keys.F4)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    Console.WriteLine("Terminate key pressed");

                    if (_processMonitor.GetRunningProcesses().Count == 0) return;

                    if (_processMonitor.GetRunningProcesses().Count == 1 && _killerButtons.Count == 1)
                    {
                        _killerButtons.First().PerformClick();
                    }
                    else
                    {
                        Button buttonToClick = null;
                        foreach (var processButton in _killerButtons)
                        {
                            if (!processButton.Focused) continue;
                            buttonToClick = processButton;
                            break;
                        }
                        buttonToClick?.PerformClick();
                    }
                }));
            }
        }

        private void SetProcessButtonActiveByProcessId(uint pid)
        {
            if (_processMonitor.GetRunningProcesses().Count < 2) return;

            foreach (var processButton in _killerButtons)
            {
                if (processButton.Process.Id == pid)
                {
                    Console.WriteLine($"Process [{pid}] becomes active");
                    processButton.Focus();
                    processButton.BackColor = ColorTranslator.FromHtml("#87D37C");
                }
                else
                {
                    processButton.BackColor = _buttonDefaultBackColor;
                }
            }
        }
    }
}
