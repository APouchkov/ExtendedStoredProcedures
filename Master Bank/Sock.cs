using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Sockets
{
    public class Sock
    {
            //private static IPHostEntry ipHost;   
            private static IPAddress   ipAddr;   
            private static IPEndPoint  ipEndPoint; 

            private const int DataSize = 100;
            public static Socket Client;    
            ~Sock()
            {
                try
                {
                    Client.Shutdown(SocketShutdown.Both);
                    Client.Close();
                }
                catch { }
            }
            public static bool IsConnected = false;
            
            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void CreateSocket(string HostName, int Port, bool IsServerApp)
            {
                //ipHost = Dns.GetHostEntry(HostName);
                //ipAddr = ipHost.AddressList[0];

                ipAddr = Dns.GetHostAddresses(HostName)[0];
                ipEndPoint = new IPEndPoint(ipAddr, Port);
             
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client.Blocking = true;
                Client.ReceiveTimeout = 60000;
                Client.SendTimeout = 60000;

                while (true)
                {
                    Client.Connect(ipEndPoint);
                    if (Client.Connected)
                    {
                        IsConnected = true;
                        break;
                    }
                }
                    
            }
                    
            [MethodImpl(MethodImplOptions.Synchronized)]
            public static byte[] GetDataFromServer()
            {
                byte[] GetBytes = new byte[DataSize];
                int nBytes = Client.Receive(GetBytes);
                Array.Resize(ref GetBytes, nBytes);
                return GetBytes;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public static void SendDataToServer(byte[] Data)
            {
                byte[] tmp = new byte[DataSize];
                int CurSize;
                int k = 0;
                
                while (k < Data.Length)
                {
                    CurSize = (Data.Length - k > DataSize) ? DataSize : Data.Length - k;
                    if (CurSize != tmp.Length)
                    {
                        Array.Resize(ref tmp, CurSize);
                    };
                    for (int i = 0; i < CurSize; i++)
                    {
                        tmp[i] = Data[k];
                        k++;
                    }
                    Client.Send(tmp);
                }                               
            }        
        }

}
