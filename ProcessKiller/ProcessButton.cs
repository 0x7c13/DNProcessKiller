
namespace ProcessKiller
{
    using System;
    using System.Threading;
    using System.Windows.Forms;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Properties;
    using System.Drawing;

    class ProcessButton : Button
    {
        public Process Process;
        private string _defaultText;
        private bool _isHighlighted;
        private bool _showPerformanceCounter;
        private readonly Color _highlightColor;
        private readonly Color _unhighlightColor;

        public ProcessButton(Color highlightColor, Color unhighlightColor)
        {
            _highlightColor = highlightColor;
            _unhighlightColor = unhighlightColor;
        }

        public void ShowPerformanceCounter()
        {
            _showPerformanceCounter = true;
            _defaultText = this.Text;

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
                            this.Text = _defaultText + Environment.NewLine + displayText;
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
            this.HidePerformanceCounter();
            this.Text = Resources.MainForm_button_click_KillingProcessMessage;
            this.Process.Kill();
        }

        public void Highlight()
        {
            _isHighlighted = true;
            this.Select();
            this.BackColor = _highlightColor;
        }

        public void Unhighlight()
        {
            _isHighlighted = false;
            this.BackColor = _unhighlightColor;
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
    }
}
