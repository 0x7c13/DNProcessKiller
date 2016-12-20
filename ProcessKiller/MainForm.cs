
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

    public partial class MainForm : Form
    {
        private readonly ProcessMonitor _processMonitor;
        private Label _processNotFoundLabel, _copyrightLabel, _versionInfoLabel;
        private TimerButton _timerButton;
        private readonly FlowLayoutPanel _container;
        private FlowLayoutPanel _buttonsContainer;
        private readonly WinEventHelper _winEvent;
        private readonly KeyboardInputEventHelper _keyboardEventHook;

        private const int DefaultProcessButtonHeight = 120;
        private const int DefaultTimerButtonHeight = 100;
        private const int DefaultProcessKillerButtonHeight = 50;
        private const int DefaultProcessNotFoundLabelHeight = 90;
        private const int DefaultCopyrightLabelHeight = 20;
        private const int DefaultVersionInfoLabelHeight = 20;
        private const int DefaultClientRectangleWidth = 250;

        private const int DefaultCountDownInSeconds = 90;
        private const int DefaultCountDownAlertInSeconds = 15;

        private readonly Color _buttonDisabledBackColor = Color.LightGray;

        private readonly Color _processButtonBackColor = ControlPaint.Light(ColorTranslator.FromHtml("#89c4f4"), 0.9f);
        private readonly Color _processButtonHighlightedBackColor = ColorTranslator.FromHtml("#4b77b1");

        private readonly Color _processKillerBackColor = ControlPaint.Light(ColorTranslator.FromHtml("#f1a9a0"), 0.9f);
        private readonly Color _processKillerHighlightedBackColor = ColorTranslator.FromHtml("#e26a6a");

        private readonly Color _timerDefaultTextColor = Color.Black;
        private readonly Color _timerAlertTextColor = Color.DarkRed;
        private readonly Color _timerDefaultBackColor = default(Color);
        private readonly Color _timerRunningBackColor = ControlPaint.Light(ColorTranslator.FromHtml("#87d37c"));

        private readonly FontFamily _defaultFontFamily;

        private static readonly object _locker = new object();
        private bool _clickToKillProcess = false;

        public MainForm()
        {
            InitializeComponent();

            _defaultFontFamily = FontFamily.GenericSansSerif;

            try
            {
                _defaultFontFamily = new FontFamily("Microsoft YaHei"); // use Microsoft YaHei if it is available
            }
            catch (Exception)
            {
                // ignore
            }

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
        }

        private Label GetProcessNotFoundLabel(int width)
        {
            return new Label
            {
                Width = width,
                Height = DefaultProcessNotFoundLabelHeight,
                Text = Resources.MainForm_InitializeDefaultTextLabel_DefaultTextLabelMessage,
                Font = new Font(_defaultFontFamily, 10, FontStyle.Regular),
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
                Font = new Font(_defaultFontFamily, 9, FontStyle.Regular),
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
                Font = new Font(_defaultFontFamily, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
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
                Margin = new Padding(3, 5, 3, 1),
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
                Margin = new Padding(6, 3, 6, 3),
                Text = countDown.TotalSeconds.ToString("0"),
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += timer_button_click;

            return button;
        }

        private void InitializeControls(FlowLayoutPanel container)
        {
            var width = container.Width - container.Margin.All * 2;

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
                var killerButton = GetProcessKillerButton(button);
                _buttonsContainer.Controls.Add(button);
                _buttonsContainer.Controls.Add(killerButton);
                button.ShowPerformanceCounter();
            }
            _container.Controls.Add(_buttonsContainer);

            _timerButton = GetTimerButton(width - container.Margin.All * 2, 
                TimeSpan.FromSeconds(DefaultCountDownInSeconds), TimeSpan.FromSeconds(DefaultCountDownAlertInSeconds));
            _container.Controls.Add(_timerButton);

            _copyrightLabel = GetCopyrightLabel(width);
            container.Controls.Add(_copyrightLabel);
            _versionInfoLabel = GetVersionInfoLabel(width);
            container.Controls.Add(_versionInfoLabel);

            ResizeWindowIfNeeded();
        }

        private void SetProcessButtonActiveByProcessId(uint pid)
        {
            foreach (var processButton in _buttonsContainer.Controls.OfType<ProcessButton>())
            {
                if (processButton.Process.Id == pid)
                {
                    Console.WriteLine($"Process [{pid}] becomes active");
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

        private void process_button_added(object sender, ControlEventArgs e)
        {
            if (IsDisposed || !this.IsHandleCreated) return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                _processNotFoundLabel.Hide();
            }));
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
                var pid = WinEventHelper.GetForegroundWindowThreadProcessId();
                Console.WriteLine(pid);
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

        private void process_button_click(object sender, EventArgs e)
        {
            try
            {
                var processButton = sender as ProcessButton;
                if (processButton == null) return;

                if (_clickToKillProcess)
                {
                    processButton.KillProcess();
                }
                else
                {
                    processButton.Highlight();
                    WinEventHelper.BringProcessToFront(processButton.Process);
                }
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
            if (IsDisposed || !this.IsHandleCreated) return;

            if (e.KeyCode == Keys.End)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    Console.WriteLine("Terminate key pressed");

                    if (_buttonsContainer.Controls.Count == 0) return;
                
                    var processButtons = _buttonsContainer.Controls.OfType<ProcessButton>().ToList();
                    var processKillerButtons = _buttonsContainer.Controls.OfType<ProcessKillerButton>().ToList();

                    var buttonToClick = processButtons.FirstOrDefault(processButton => processButton.IsHighlighted());
                    if (buttonToClick == null) return;
                    var killerButtonToClick = processKillerButtons.FirstOrDefault(processButton => processButton.ProcessButton.Process.Id == buttonToClick.Process.Id);
                    killerButtonToClick?.PerformClick();
                }));
            }
            else if (e.KeyCode == Keys.F5)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    Console.WriteLine("Timer key pressed");

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

        private void resize_end(object sender, EventArgs e)
        {
            ResizeWindowIfNeeded();
        }
    }
}
