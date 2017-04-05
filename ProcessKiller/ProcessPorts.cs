
namespace ProcessKiller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Static class that returns the list of processes and the ports those processes use.
    /// </summary>
    public static class ProcessPorts
    {
        /// <summary>
        /// A list of ProcesesPorts that contain the mapping of processes and the ports that the process uses.
        /// </summary>
        public static List<ProcessPort> ProcessPortMap(int pid)
        {
            return GetNetStatPorts(pid);
        }


        /// <summary>
        /// This method distills the output from netstat -a -n -o into a list of ProcessPorts that provide a mapping between
        /// the process (name and id) and the ports that the process is using.
        /// </summary>
        /// <returns></returns>
        private static List<ProcessPort> GetNetStatPorts(int pid)
        {
            var processPorts = new List<ProcessPort>();

            try
            {
                using (Process Proc = new Process())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c netstat.exe -a -n -o | find \"{pid}\"" ,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    };

                    Proc.StartInfo = startInfo;
                    Proc.Start();

                    var standardOutput = Proc.StandardOutput;
                    var standardError = Proc.StandardError;

                    string netStatContent = standardOutput.ReadToEnd() + standardError.ReadToEnd();
                    string netStatExitStatus = Proc.ExitCode.ToString();

                    if (netStatExitStatus != "0")
                    {
                        Console.WriteLine("NetStat command failed.   This may require elevated permissions.");
                    }

                    string[] netStatRows = Regex.Split(netStatContent, "\r\n");

                    foreach (string NetStatRow in netStatRows)
                    {
                        string[] Tokens = Regex.Split(NetStatRow, "\\s+");
                        if (Tokens.Length > 4 && (Tokens[1].Equals("UDP") || Tokens[1].Equals("TCP")))
                        {
                            string ipAddress = Regex.Replace(Tokens[2], @"\[(.*?)\]", "1.1.1.1");
                            try
                            {
                                processPorts.Add(new ProcessPort(
                                    Tokens[1] == "UDP" ? GetProcessName(Convert.ToInt32(Tokens[4])) : GetProcessName(Convert.ToInt32(Tokens[5])),
                                    Tokens[1] == "UDP" ? Convert.ToInt32(Tokens[4]) : Convert.ToInt32(Tokens[5]),
                                    ipAddress.Contains("1.1.1.1") ? $"{Tokens[1]}v6" : $"{Tokens[1]}v4",
                                    Convert.ToInt32(ipAddress.Split(':')[1])
                                ));
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Could not convert the following NetStat row to a Process to Port mapping.");
                                Console.WriteLine(NetStatRow);
                                Console.WriteLine("Reason: " + ex);
                            }
                        }
                        else
                        {
                            if (!NetStatRow.Trim().StartsWith("Proto") && !NetStatRow.Trim().StartsWith("Active") && !String.IsNullOrWhiteSpace(NetStatRow))
                            {
                                Console.WriteLine("Unrecognized NetStat row to a Process to Port mapping.");
                                Console.WriteLine(NetStatRow);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return processPorts;
        }

        /// <summary>
        /// Private method that handles pulling the process name (if one exists) from the process id.
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        private static string GetProcessName(int processId)
        {
            var procName = "UNKNOWN";

            try
            {
                procName = Process.GetProcessById(processId).ProcessName;
            }
            catch {
                // ignore 
            }

            return procName;
        }
    }

    /// <summary>
    /// A mapping for processes to ports and ports to processes that are being used in the system.
    /// </summary>
    public class ProcessPort
    {
        /// <summary>
        /// Internal constructor to initialize the mapping of process to port.
        /// </summary>
        /// <param name="processName">Name of process to be </param>
        /// <param name="processId"></param>
        /// <param name="protocol"></param>
        /// <param name="portNumber"></param>
        internal ProcessPort(string processName, int processId, string protocol, int portNumber)
        {
            this.ProcessName = processName;
            this.ProcessId = processId;
            this.Protocol = protocol;
            this.PortNumber = portNumber;
        }

        public string ProcessPortDescription => $"{ProcessName} ({Protocol} port {PortNumber} pid {ProcessId})";

        public string ProcessName { get; }

        public int ProcessId { get; } = 0;

        public string Protocol { get; }

        public int PortNumber { get; } = 0;
    }
}
