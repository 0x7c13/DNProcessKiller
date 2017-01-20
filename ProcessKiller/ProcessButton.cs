
namespace ProcessKiller
{
    using System;
    using System.Threading;
    using System.Windows.Forms;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Drawing;
    using System.Management;

    class ProcessButton : Button
    {
        public Process Process;
        private readonly string _defaultText;
        private readonly string _highlightedText;
        private bool _isHighlighted;
        private bool _showPerformanceCounter;
        private readonly Color _highlightColor;
        private readonly Color _unhighlightColor;
        private readonly Color _disableColor;
        private readonly string _killingMessage;

        public ProcessButton(Color highlightColor, Color unhighlightColor, Color disableColor, string killingMessage, string defaultText, string highlightedText)
        {
            SetStyle(ControlStyles.Selectable, false);
            _highlightColor = highlightColor;
            _unhighlightColor = unhighlightColor;
            _disableColor = disableColor;
            _killingMessage = killingMessage;
            _defaultText = defaultText;
            _highlightedText = highlightedText;
        }

        public void ShowPerformanceCounter()
        {
            _showPerformanceCounter = true;

            Task.Run(() =>
            {
                StartPerformanceCounter();
            });
        }

        public void StartPerformanceCounter()
        {
            string processName = null;
            PerformanceCounter ramCounter = null;
            PerformanceCounter cpuCounter = null;

            // init performance counters
            while (!IsDisposed && _showPerformanceCounter)
            {
                try
                {
                    processName = GetProcessInstanceName(Process.Id);
                    ramCounter = new PerformanceCounter("Process", "Working Set", processName);
                    cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);
                    break;
                }
                catch (Exception)
                {
                    // retry
                }

                Thread.Sleep(1000);
            }

            // update performance counters
            while (!IsDisposed && _showPerformanceCounter)
            {
                try
                {
                    var ram = (long)ramCounter.NextValue();
                    var cpu = (int)cpuCounter.NextValue();

                    var displayText = "内存: " + (ram / 1024 / 1024) + " MB - CPU: " + (cpu / Environment.ProcessorCount) + " %";

                    if (this.IsHandleCreated)
                    {
                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            this.Text = (_isHighlighted ? _highlightedText : _defaultText) + Environment.NewLine + displayText;
                        }));
                    }
                }
                catch (InvalidOperationException ex)
                {
                    try
                    {
                        if (!string.Equals(processName, GetProcessInstanceName(Process.Id)))
                        {
                            processName = GetProcessInstanceName(Process.Id);
                            ramCounter = new PerformanceCounter("Process", "Working Set", processName);
                            cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);
                        }
                    }
                    catch (Exception)
                    {
                        // ignore
                    }

                    Console.WriteLine(ex);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Thread.Sleep(1000);
            }
        }

        public void HidePerformanceCounter()
        {
            _showPerformanceCounter = false;
            this.Text = _defaultText;
        }

        public void KillProcess()
        {
            this.Enabled = false;
            this.BackColor = _disableColor;
            this.HidePerformanceCounter();
            this.Text = _killingMessage;
            KillProcessAndChildren(this.Process.Id);
        }

        public void Highlight()
        {
            if (!Enabled) return;
            _isHighlighted = true;
            this.ForeColor = Color.White;
            this.BackColor = _highlightColor;
            //this.Text = _highlightedText;
            this.Select();
        }

        public void Unhighlight()
        {
            if (!Enabled) return;
            _isHighlighted = false;
            this.ForeColor = Color.Black;
            this.BackColor = _unhighlightColor;
            //this.Text = _defaultText;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        private static string GetProcessInstanceName(int pid)
        {
            var cat = new PerformanceCounterCategory("Process");

            var instances = cat.GetInstanceNames();
            foreach (var instance in instances)
            {
                using (var cnt = new PerformanceCounter("Process",
                     "ID Process", instance, true))
                {
                    int val = (int)cnt.RawValue;
                    if (val == pid)
                    {
                        return instance;
                    }
                }
            }
            throw new Exception("Could not find performance counter " +
                "instance name for current process. This is truly strange ...");
        }

        private void KillProcessAndChildren(int pid)
        {
            var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();
            foreach (var mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                this.Process.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        protected override bool ShowFocusCues => false;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw Border using color specified in Flat Appearance
            Pen pen = new Pen(Color.Gray, 1);
            Rectangle rectangle = new Rectangle(0, 0, Size.Width - 1, Size.Height - 1);
            e.Graphics.DrawRectangle(pen, rectangle);
        }
    }
}
