
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
        private Label _processNotFoundLabel, _copyrightLabel, _versionInfoLabel;
        private readonly FlowLayoutPanel _container;
        private FlowLayoutPanel _buttonsContainer;
        private readonly WinEventHookHelper _winEventHook;
        private readonly KeyboardInputEventHelper _keyboardEventHook;

        private const int DefaultProcessButtonHeight = 150;
        private const int DefaultProcessNotFoundLabelHeight = 90;
        private const int DefaultCopyrightLabelHeight = 20;
        private const int DefaultVersionInfoLabelHeight = 20;
        private const int DefaultClientRectangleWidth = 250;
        private readonly Color _buttonDefaultBackColor = ColorTranslator.FromHtml("#ecf0f1");

        private static readonly object _locker = new object();

        public MainForm()
        {
            InitializeComponent();

            _processMonitor = new ProcessMonitor("dragonnest");
            //_processMonitor = new ProcessMonitor("notepad");

            _processMonitor.OnEventArrived += event_arrived;
            _container = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = DefaultClientRectangleWidth,
            };

            this.Controls.Add(_container);

            InitializeControls(_container);

            _processMonitor.StartMonitoring();

            _winEventHook = new WinEventHookHelper();
            _winEventHook.ProcessEnterForeground += process_enter_foreground;

            _keyboardEventHook = new KeyboardInputEventHelper();
            _keyboardEventHook.KeyBoardKeyDownEvent += keyboard_key_down;

            this.ResizeEnd += resize_end;

            // hacky way to keep delegates alive
            GC.KeepAlive(_winEventHook);
            GC.KeepAlive(_keyboardEventHook);
        }

        private void resize_end(object sender, EventArgs e)
        {
            ResizeWindowIfNeeded();
        }

        private Label GetProcessNotFoundLabel(int width)
        {
            return new Label
            {
                Width = width,
                Height = DefaultProcessNotFoundLabelHeight,
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
                Height = DefaultCopyrightLabelHeight,
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
                Height = DefaultVersionInfoLabelHeight,
                Text = Resources.MainForm_InitializeVersionLabel_Version,
                Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
            };
        }

        private ProcessButton GetProcessButton(Process process)
        {
            var button = new ProcessButton
            {
                Width = DefaultClientRectangleWidth - (_buttonsContainer.Padding.Left + _buttonsContainer.Margin.Left + _container.Padding.Left + _container.Margin.Left) * 2,
                Height = DefaultProcessButtonHeight,
                Text = Resources.MainForm_InitializeButtons_ClickToKillProcessMessage + " (PID:" + process.Id + ")",
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular),
                BackColor = _buttonDefaultBackColor,
                Process = process,
            };
            button.Click += process_button_click;
            
            return button;
        }

        private void InitializeControls(FlowLayoutPanel container)
        {
            var width = container.Width;

            _processNotFoundLabel = GetProcessNotFoundLabel(width);

            _buttonsContainer = new FlowLayoutPanel()
            {
                AutoSize = true,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
            };
            _buttonsContainer.ControlAdded += process_button_added;
            _buttonsContainer.ControlRemoved += process_button_removed;

            if (_processMonitor.GetRunningProcesses().Count > 0)
            {
                _processNotFoundLabel.Hide();
            }
            container.Controls.Add(_processNotFoundLabel);

            for (var i = 0; i < _processMonitor.GetRunningProcesses().Count; i++)
            {
                var process = _processMonitor.GetRunningProcesses()[i];
                var button = GetProcessButton(process);
                _buttonsContainer.Controls.Add(button);
                button.ShowPerformanceCounter();
            }
            _container.Controls.Add(_buttonsContainer);
            
            _copyrightLabel = GetCopyrightLabel(width);
            container.Controls.Add(_copyrightLabel);
            _versionInfoLabel = GetVersionInfoLabel(width);
            container.Controls.Add(_versionInfoLabel);

            ResizeWindowIfNeeded();
        }

        private void process_button_removed(object sender, ControlEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            if (_buttonsContainer.Controls.Count == 0)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    _processNotFoundLabel.Show();
                }));
            }
            else
            {
                var pid = WinEventHookHelper.GetForegroundWindowThreadProcessId();
                Console.WriteLine(pid);
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    SetProcessButtonActiveByProcessId(pid);
                }));
            }
        }

        private void process_button_added(object sender, ControlEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                _processNotFoundLabel.Hide();
            }));
        }

        private void ResizeWindowIfNeeded()
        {
            var height = (from Control control in _container.Controls select control.Height).Sum() + _container.Margin.All * (_container.Controls.Count + 2);
            if (_buttonsContainer.Controls.Count > 0)
            {
                height -= DefaultProcessNotFoundLabelHeight;
                height -= _container.Margin.All;
            }

            if (DefaultClientRectangleWidth != this.ClientSize.Width || height != this.ClientSize.Height)
            {
                this.ClientSize = new Size(DefaultClientRectangleWidth, height);
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
                        var button = GetProcessButton(Process.GetProcessById((int) pid));
                        _buttonsContainer?.Controls.Add(button);
                        button.ShowPerformanceCounter();
                        SetProcessButtonActiveByProcessId(pid);
                    }
                    else if (type == ProcessEventType.Stop)
                    {
                        var buttonToRemove = _buttonsContainer.Controls.Cast<ProcessButton>().FirstOrDefault(button => button.Process.Id == pid);
                        if (buttonToRemove == null) return;

                        buttonToRemove.HidePerformanceCounter();
                        _buttonsContainer?.Controls.Remove(buttonToRemove);
                        buttonToRemove.Dispose();
                    }
                    ResizeWindowIfNeeded();
                }));
            }
        }

        private void process_button_click(object sender, EventArgs e)
        {
            try
            {
                var processButton = sender as ProcessButton;
                if (processButton == null) return;

                processButton.Enabled = false;
                processButton.HidePerformanceCounter();
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
            if (IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                SetProcessButtonActiveByProcessId(pid);
            }));
        }

        void keyboard_key_down(object sender, KeyEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            if (e.KeyCode == Keys.F4)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    Console.WriteLine("Terminate key pressed");

                    if (_buttonsContainer.Controls.Count == 0) return;

                    if (_buttonsContainer.Controls.Count == 1)
                    {
                        ((ProcessButton)_buttonsContainer.Controls[0]).PerformClick();
                    }
                    else
                    {
                        Button buttonToClick = _buttonsContainer.Controls.Cast<ProcessButton>().FirstOrDefault(processButton => processButton.Focused);
                        buttonToClick?.PerformClick();
                    }
                }));
            }
        }

        private void SetProcessButtonActiveByProcessId(uint pid)
        {
            foreach (ProcessButton processButton in _buttonsContainer.Controls)
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
