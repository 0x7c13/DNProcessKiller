
namespace ProcessKiller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Properties;

    public class Server
    {
        public string Name { get; set; }

        public string IpAddress { get; set; }

        public string Port { get; set; }
    }

    public class DNServerInfo
    {
        public static IList<Server> GetServerListFromResource()
        {
            return Resources.servers.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)).Select(parts => new Server()
            {
                IpAddress = parts[0], Port = parts[1], Name = parts[2],
            }).ToList();
        }
    }
}
