
namespace ProcessKiller
{
    using System;
    using System.Threading;
    using System.Windows.Forms;
    using System.Diagnostics;
    using System.Threading.Tasks;

    class ProcessButton : Button
    {
        public Process Process;
        private string _defaultText;

        public void ShowPerformanceCounter()
        {
            _defaultText = this.Text;

            string processName = null;
            try
            {
                processName = GetProcessInstanceName(Process.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            var ramCounter = new PerformanceCounter("Process", "Working Set", processName);
            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", processName);

            Task.Run(() =>
            {
                while (true)
                {
                    if (IsDisposed) return;

                    try
                    {
                        var ram = (int) ramCounter.NextValue();
                        var cpu = (int) cpuCounter.NextValue();
                        var displayText = "内存: " + (ram / 1024 / 1024) + " MB - CPU: " + (cpu / Environment.ProcessorCount) + " %";
                        //Console.WriteLine(displayText);

                        this.BeginInvoke(new MethodInvoker(() =>
                        {
                            this.Text = _defaultText + " " + displayText;
                        }));
                    }
                    catch (InvalidOperationException)
                    {
                        // ignore
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    Thread.Sleep(1000);
                }
            });
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
