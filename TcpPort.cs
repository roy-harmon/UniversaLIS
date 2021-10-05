using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace UniversaLIS
{
     partial class TcpPort : IComPort
     {
          // TODO: Use this class to implement TCP/IP connections, possibly in the same way CommPort is used for serial connections.
          // This shouldn't be hard if each connection uses a different port, though that feels a little lazy.
          TcpListener server = null;
          Socket client = null;
          NetworkStream stream = null;
          bool isActive;
          byte[] bytes = new byte[64000];
          string data = null;
          public TcpPort(Tcp tcpSettings)
          {
               try
               {
                    int port = tcpSettings.Socket;
                    IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                    server = new TcpListener(localAddr, port);
                    server.Start(1); // Only one instrument connection per port, for simplicity.
                    isActive = true;
               }
               catch (Exception)
               {
                    
                    throw;
               }
          }

          void IComPort.Close()
          {
               if (!(client is null))
               {
                    client.Close();
               }
               server.Stop();
               isActive = false;
          }

          void IComPort.Open()
          {
               if (!(server is null))
               {
                    if (!isActive)
                    {
                         server.Start(1);
                    }
                    while (true)
                    {
                         if (server.Pending())
                         {
                              client = server.AcceptSocket();
                              data = null;
                              //stream = client.GetStream();
                              break;
                         }
                    }
                    while (client.Connected)
                    {

                         int i;
                         while ((i = client.Receive(bytes))!=0)
                         {
                              data = Encoding.UTF8.GetString(bytes, 0, i);
                         }
                    }
               }
          }

          // Send/Receive asynchronously? Use two threads?

          //int IComPort.Read(byte[] buffer, int offset, int count)
          //{
          //     throw new NotImplementedException();
          //}

          void IComPort.Send()
          {
               throw new NotImplementedException();
          }

          //void IComPort.Write(byte[] buffer, int offset, int count)
          //{
          //     throw new NotImplementedException();
          //}
     }
}
