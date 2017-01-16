
namespace ProcessKiller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Management;
    using System.Diagnostics;

    public enum ProcessEventType
    {
        Start = 0,
        Stop,
    }

    public class ProcessMonitor : IDisposable
    {
        public delegate void EventArrivedEventHandler(ProcessEventType type, uint pid);
        public event EventArrivedEventHandler OnEventArrived;

        private readonly string _processName;
        private readonly List<Process> _runningProcesses;
        private ManagementEventWatcher _processStartWatcher;
        private ManagementEventWatcher _processStopWatcher;
        private bool _disposed;

        private static readonly object _locker = new object();

        public ProcessMonitor(string processName)
        {
            _processName = processName;
            _runningProcesses = new List<Process>();

            var allProcess = Process.GetProcesses();
            foreach (var process in allProcess)
            {
                if (process.ProcessName.ToLower().Contains(processName.ToLower()))
                {
                    _runningProcesses.Add(process);
                }
            }
            Console.WriteLine($"Found {_runningProcesses.Count} instance running");
        }

        public void StartMonitoring()
        {
            StartWatcher();
        }

        public void StopMonitoring()
        {
            lock (_locker)
            {
                _runningProcesses.Clear();
            }

            StopWatcher();
        }

        public IList<Process> GetRunningProcesses()
        {
            return _runningProcesses;
        }

        private void StartWatcher()
        {
            // setup start event watcher
            {
                _processStartWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                _processStartWatcher.EventArrived += startWatch_EventArrived;
                _processStartWatcher.Start();
            }
            // setup stop event watcher
            {
                _processStopWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                _processStopWatcher.EventArrived += stopWatch_EventArrived;
                _processStopWatcher.Start();
            }
        }

        private void StopWatcher()
        {
            _processStartWatcher.Stop();
            _processStopWatcher.Stop();
        }

        private void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var pName = Path.GetFileNameWithoutExtension(e.NewEvent.Properties["ProcessName"].Value.ToString());
                var pId = uint.Parse(e.NewEvent.Properties["ProcessId"].Value.ToString());

                if (pName.ToLower().Contains(_processName.ToLower()))
                {
                    Console.WriteLine($"Process started: {pName} [{pId}]", pName);
                    var process = Process.GetProcessById((int)pId);
                    lock (_locker)
                    {
                        _runningProcesses.Add(process);
                        OnEventArrived?.Invoke(ProcessEventType.Start, pId);
                    }
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine(ex);
            }
        }

        private void stopWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var pName = Path.GetFileNameWithoutExtension(e.NewEvent.Properties["ProcessName"].Value.ToString());
                var pId = uint.Parse(e.NewEvent.Properties["ProcessId"].Value.ToString());

                if (pName.ToLower().Contains(_processName.ToLower()))
                {
                    Console.WriteLine($"Process stopped: {pName} [{pId}]", pName);

                    lock (_locker)
                    {
                        var processToRemove = _runningProcesses.FirstOrDefault(process => process.Id == pId);

                        if (processToRemove != null)
                        {
                            _runningProcesses.Remove(processToRemove);
                            OnEventArrived?.Invoke(ProcessEventType.Stop, pId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopWatcher();
            _disposed = true;
        }

        ~ProcessMonitor()
        {
            Dispose();
        }
    }
}
