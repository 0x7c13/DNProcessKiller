
namespace ProcessKiller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using Microsoft.Win32;

    public class MutexAppLauncher
    {
        private readonly string _handleExePath;
        private readonly string _appName;

        public MutexAppLauncher(string handleExePath, string appName)
        {
            if (!File.Exists(handleExePath))
            {
                throw new ArgumentException("hanlde.exe not found");
            }

            _handleExeRegistry();
            _handleExePath = handleExePath;
            _appName = appName;
        }

        private void _handleExeRegistry()
        {
            var reg = Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Sysinternals\\Handle", "EulaAccepted", null);
            if ((reg == null || reg.ToString() == "0"))
            {
                Registry.SetValue("HKEY_CURRENT_USER\\SOFTWARE\\Sysinternals\\Handle", "EulaAccepted", 1);
            }
        }

        public void Launch(string workingDir, string fileName, string args)
        {
            string pid, closeHanldeArg;

            if (_shouldCloseHandle(_getHandleOutput(), out  pid, out closeHanldeArg))
            {
                var handleProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = _handleExePath,
                        Arguments = $"-p {pid} -c {closeHanldeArg} -y -nobanner",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    }
                };
                handleProcess.Start();
                handleProcess.WaitForExit();
                handleProcess.Close();
            }
            else
            {
                // no matching handles were found.
            }

            LaunchApp(workingDir, fileName, args);
        }

        private bool _shouldCloseHandle(string output, out string pid, out string closeHanldeArg)
        {
            pid = null;
            closeHanldeArg = null;

            var regex = new Regex($"pid: (\\d+)\\s+type: Mutant\\s+(\\w+): \\\\BaseNamedObjects\\\\Mutex{_appName}");
            if (regex.IsMatch(output))
            {
                var match = regex.Match(output);
                pid = match.Result("$1");
                closeHanldeArg = match.Result("$2");
                return true;
            }
            return false;
        }

        private string _getHandleOutput()
        {
            var handleProcess = new Process
            {
                StartInfo =
                {
                    FileName = _handleExePath,
                    Verb = "runas",
                    Arguments = $"/a Mutex{_appName} /nobanner",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            handleProcess.Start();
            var output = handleProcess.StandardOutput.ReadToEnd();
            handleProcess.WaitForExit();
            handleProcess.Close();
            return output;
        }

        private static void LaunchApp(string workingDir, string fileName, string args)
        {
            var appInstance = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(workingDir, fileName),
                    Arguments = args,
                    UseShellExecute = false,
                    WorkingDirectory = workingDir
                }
            };
            appInstance.Start();
            appInstance.Close();
        }
    }
}
