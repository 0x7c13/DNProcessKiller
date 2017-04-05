
namespace ProcessKiller
{
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;


    public class Disconnecter
    {

        /// <summary> 
        /// Enumeration of the states 
        /// </summary> 
        public enum State
        {
            /// <summary> All </summary> 
            All = 0,
            /// <summary> Closed </summary> 
            Closed = 1,
            /// <summary> Listen </summary> 
            Listen = 2,
            /// <summary> Syn_Sent </summary> 
            Syn_Sent = 3,
            /// <summary> Syn_Rcvd </summary> 
            Syn_Rcvd = 4,
            /// <summary> Established </summary> 
            Established = 5,
            /// <summary> Fin_Wait1 </summary> 
            Fin_Wait1 = 6,
            /// <summary> Fin_Wait2 </summary> 
            Fin_Wait2 = 7,
            /// <summary> Close_Wait </summary> 
            Close_Wait = 8,
            /// <summary> Closing </summary> 
            Closing = 9,
            /// <summary> Last_Ack </summary> 
            Last_Ack = 10,
            /// <summary> Time_Wait </summary> 
            Time_Wait = 11,
            /// <summary> Delete_TCB </summary> 
            Delete_TCB = 12
        }

        /// <summary> 
        /// Connection info 
        /// </summary> 
        private struct MIB_TCPROW
        {
            public int dwState;
            public int dwLocalAddr;
            public int dwLocalPort;
            public int dwRemoteAddr;
            public int dwRemotePort;
        }

        //API to get list of connections 
        [DllImport("iphlpapi.dll")]
        private static extern int GetTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder);

        //API to change status of connection 
        [DllImport("iphlpapi.dll")]
        //private static extern int SetTcpEntry(MIB_TCPROW tcprow); 
        private static extern int SetTcpEntry(IntPtr pTcprow);

        //Convert 16-bit value from network to host byte order 
        [DllImport("wsock32.dll")]
        private static extern int ntohs(int netshort);

        //Convert 16-bit value back again 
        [DllImport("wsock32.dll")]
        private static extern int htons(int netshort);

        /// <summary> 
        /// Testexample 
        /// </summary> 
        public static void TEST()
        {
            //string[] ret = Connections(State.All);
            //foreach (string con in ret) if (con.IndexOf("192.168.0.101") > -1) System.Diagnostics.Debug.WriteLine(con);
            //System.Diagnostics.Debug.WriteLine("----------------------------");
            //CloseRemotePort(1863);
            //CloseRemotePort(80);
            //ret = Connections(State.All);
            //foreach (string con in ret) if (con.IndexOf("192.168.0.101") > -1) System.Diagnostics.Debug.WriteLine(con);
        }

        /// <summary> 
        /// Close all connection to the remote IP 
        /// </summary> 
        /// <param name="IP">IPen på remote PC</param> 
        public static void CloseRemoteIP(string IP)
        {
            MIB_TCPROW[] rows = getTcpTable();
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].dwRemoteAddr == IPStringToInt(IP))
                {
                    rows[i].dwState = (int)State.Delete_TCB;
                    IntPtr ptr = GetPtrToNewObject(rows[i]);
                    int ret = SetTcpEntry(ptr);
                }
            }
        }

        /// <summary> 
        /// Close all connections at current local IP 
        /// </summary> 
        /// <param name="IP"></param> 
        public static void CloseLocalIP(string IP)
        {
            MIB_TCPROW[] rows = getTcpTable();
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].dwLocalAddr == IPStringToInt(IP))
                {
                    rows[i].dwState = (int)State.Delete_TCB;
                    IntPtr ptr = GetPtrToNewObject(rows[i]);
                    int ret = SetTcpEntry(ptr);
                }
            }
        }

        /// <summary> 
        /// Closes all connections to the remote port 
        /// </summary> 
        /// <param name="port"></param> 
        public static void CloseRemotePort(int port)
        {
            MIB_TCPROW[] rows = getTcpTable();
            for (int i = 0; i < rows.Length; i++)
            {
                if (port == ntohs(rows[i].dwRemotePort))
                {
                    rows[i].dwState = (int)State.Delete_TCB;
                    IntPtr ptr = GetPtrToNewObject(rows[i]);
                    int ret = SetTcpEntry(ptr);
                }
            }
        }

        /// <summary> 
        /// Closes all connections to the local port 
        /// </summary> 
        /// <param name="port"></param> 
        public static void CloseLocalPort(int port)
        {
            MIB_TCPROW[] rows = getTcpTable();
            for (int i = 0; i < rows.Length; i++)
            {
                if (port == ntohs(rows[i].dwLocalPort))
                {
                    rows[i].dwState = (int)State.Delete_TCB;
                    IntPtr ptr = GetPtrToNewObject(rows[i]);
                    int ret = SetTcpEntry(ptr);
                }
            }
        }

        /// <summary> 
        /// Close a connection by returning the connectionstring 
        /// </summary> 
        /// <param name="connectionstring"></param> 
        public static void CloseConnection(string connectionstring)
        {
            try
            {
                //Split the string to its subparts 
                string[] parts = connectionstring.Split('-');
                if (parts.Length != 4) throw new Exception("Invalid connectionstring - use the one provided by Connections.");
                string[] loc = parts[0].Split(':');
                string[] rem = parts[1].Split(':');
                string[] locaddr = loc[0].Split('.');
                string[] remaddr = rem[0].Split('.');

                //Fill structure with data 
                MIB_TCPROW row = new MIB_TCPROW();
                row.dwState = 12;
                byte[] bLocAddr = new byte[] { byte.Parse(locaddr[0]), byte.Parse(locaddr[1]), byte.Parse(locaddr[2]), byte.Parse(locaddr[3]) };
                byte[] bRemAddr = new byte[] { byte.Parse(remaddr[0]), byte.Parse(remaddr[1]), byte.Parse(remaddr[2]), byte.Parse(remaddr[3]) };
                row.dwLocalAddr = BitConverter.ToInt32(bLocAddr, 0);
                row.dwRemoteAddr = BitConverter.ToInt32(bRemAddr, 0);
                row.dwLocalPort = htons(int.Parse(loc[1]));
                row.dwRemotePort = htons(int.Parse(rem[1]));

                //Make copy of the structure into memory and use the pointer to call SetTcpEntry 
                IntPtr ptr = GetPtrToNewObject(row);
                int ret = SetTcpEntry(ptr);

                if (ret == -1) throw new Exception("Unsuccessful");
                if (ret == 65) throw new Exception("User has no sufficient privilege to execute this API successfully");
                if (ret == 87) throw new Exception("Specified port is not in state to be closed down");
                if (ret != 0) throw new Exception("Unknown error (" + ret + ")");

            }
            catch (Exception ex)
            {
                throw new Exception("CloseConnection failed (" + connectionstring + ")! [" + ex.GetType().ToString() + "," + ex.Message + "]");
            }
        }

        /// <summary> 
        /// Gets all connections 
        /// </summary> 
        /// <returns></returns> 
        public static string[] Connections()
        {
            return Connections(State.All);
        }

        /// <summary> 
        /// Gets a connection list of connections with a defined state 
        /// </summary> 
        /// <param name="state"></param> 
        /// <returns></returns> 
        public static string[] Connections(State state)
        {
            MIB_TCPROW[] rows = getTcpTable();

            ArrayList arr = new ArrayList();

            foreach (MIB_TCPROW row in rows)
            {
                if (state == State.All || state == (State)row.dwState)
                {
                    string localaddress = IPIntToString(row.dwLocalAddr) + ":" + ntohs(row.dwLocalPort);
                    string remoteaddress = IPIntToString(row.dwRemoteAddr) + ":" + ntohs(row.dwRemotePort);
                    arr.Add(localaddress + "-" + remoteaddress + "-" + ((State)row.dwState).ToString() + "-" + row.dwState);
                }
            }

            return (string[])arr.ToArray(typeof(System.String));
        }

        //The function that fills the MIB_TCPROW array with connectioninfos 
        private static MIB_TCPROW[] getTcpTable()
        {
            IntPtr buffer = IntPtr.Zero; bool allocated = false;
            try
            {
                int iBytes = 0;
                GetTcpTable(IntPtr.Zero, ref iBytes, false); //Getting size of return data 
                buffer = Marshal.AllocCoTaskMem(iBytes); //allocating the datasize 

                allocated = true;

                GetTcpTable(buffer, ref iBytes, false); //Run it again to fill the memory with the data 

                int structCount = Marshal.ReadInt32(buffer); // Get the number of structures 

                IntPtr buffSubPointer = buffer; //Making a pointer that will point into the buffer 
                buffSubPointer = (IntPtr)((int)buffer + 4); //Move to the first data (ignoring dwNumEntries from the original MIB_TCPTABLE struct) 

                MIB_TCPROW[] tcpRows = new MIB_TCPROW[structCount]; //Declaring the array 

                //Get the struct size 
                MIB_TCPROW tmp = new MIB_TCPROW();
                int sizeOfTCPROW = Marshal.SizeOf(tmp);

                //Fill the array 1 by 1 
                for (int i = 0; i < structCount; i++)
                {
                    tcpRows[i] = (MIB_TCPROW)Marshal.PtrToStructure(buffSubPointer, typeof(MIB_TCPROW)); //copy struct data 
                    buffSubPointer = (IntPtr)((int)buffSubPointer + sizeOfTCPROW); //move to next structdata 
                }

                return tcpRows;

            }
            catch (Exception ex)
            {
                throw new Exception("getTcpTable failed! [" + ex.GetType().ToString() + "," + ex.Message + "]");
            }
            finally
            {
                if (allocated) Marshal.FreeCoTaskMem(buffer); //Free the allocated memory 
            }
        }

        private static IntPtr GetPtrToNewObject(object obj)
        {
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
        }

        //Convert an IP string to the INT value 
        private static int IPStringToInt(string IP)
        {
            if (IP.IndexOf(".") < 0) throw new Exception("Invalid IP address");
            string[] addr = IP.Split('.');
            if (addr.Length != 4) throw new Exception("Invalid IP address");
            byte[] bytes = new byte[] { byte.Parse(addr[0]), byte.Parse(addr[1]), byte.Parse(addr[2]), byte.Parse(addr[3]) };
            return BitConverter.ToInt32(bytes, 0);
        }
        //Convert an IP integer to IP string 
        private static string IPIntToString(int IP)
        {
            byte[] addr = System.BitConverter.GetBytes(IP);
            return addr[0] + "." + addr[1] + "." + addr[2] + "." + addr[3];
        }

    }
}
