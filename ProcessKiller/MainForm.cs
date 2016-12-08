
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
        private Label _defaultTextLabel, _defaultCopyrightLabel;
        private readonly WinEventHookHelper _winEventHook;
        private readonly KeyboardInputEventHelper _keyboardEventHook;

        private static int _defaultCopyrightLabelHeight = 40;
        private static int _buttonBorder = 5;
        private static int _defaultClientRectangleWidth = 250;
        private static int _defaultClientRectangleHeight = 180;
        private readonly Color _buttonDefaultBackColor = ColorTranslator.FromHtml("#ecf0f1");

        public MainForm()
        {
            _killerButtons = new ConcurrentBag<ProcessButton>();
            InitializeComponent();

            _processMonitor = new ProcessMonitor("dragonnest");

            _processMonitor.OnEventArrived += event_arrived;
            InitializeControls();
            _processMonitor.StartMonitoring();

            _winEventHook = new WinEventHookHelper();
            _winEventHook.ProcessEnterForeground += process_enter_foreground;

            _keyboardEventHook = new KeyboardInputEventHelper();
            _keyboardEventHook.KeyBoardKeyDownEvent += keyboard_key_down;
        }

        private void InitializeDefaultTextLabel()
        {
            _defaultTextLabel = new Label
            {
                Left = _buttonBorder,
                Top = _buttonBorder,
                Width = _defaultClientRectangleWidth - _buttonBorder * 2,
                Height = _defaultClientRectangleHeight - _buttonBorder * 2,
                Text = Resources.MainForm_InitializeDefaultTextLabel_DefaultTextLabelMessage,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            this.Controls.Add(_defaultTextLabel);
        }
        private void InitializeCopyRightLabel(int top)
        {
            _defaultCopyrightLabel = new Label
            {
                Left = _buttonBorder,
                Top = top,
                Width = _defaultClientRectangleWidth - _buttonBorder * 2,
                Height = _defaultCopyrightLabelHeight,
                Text = Resources.MainForm_InitializeCopyRightLabel_CopyrightMessage,
                Font = new Font(FontFamily.GenericSansSerif, 8, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            this.Controls.Add(_defaultCopyrightLabel);
        }

        private void InitializeControls()
        {
            this.Controls.Clear();
            
            this.ClientSize = new Size(_defaultClientRectangleWidth, Math.Max(_defaultClientRectangleHeight, _defaultClientRectangleHeight * _processMonitor.GetRunningProcesses().Count) + _defaultCopyrightLabelHeight);

            _killerButtons = new ConcurrentBag<ProcessButton>();

            if (_processMonitor.GetRunningProcesses().Count == 0)
            {
                InitializeDefaultTextLabel();
                InitializeCopyRightLabel(_defaultClientRectangleHeight);
                return;
            }

            var top = _buttonBorder;
            var left = _buttonBorder;
            
            foreach (var process in _processMonitor.GetRunningProcesses())
            {
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

                _killerButtons.Add(button);
                this.Controls.Add(button);
                top += button.Height + _buttonBorder;
            }

            InitializeCopyRightLabel(top);
        }

        // Why am I so damn lazy?...
        private void event_arrived(ProcessEventType type, Process process)
        {
            if (IsDisposed) return;

            this.BeginInvoke(new MethodInvoker(() =>
            {
                InitializeControls();

                if (type == ProcessEventType.Start)
                {
                    SetProcessButtonActiveByProcessId((uint)process.Id);
                }
            }));
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
