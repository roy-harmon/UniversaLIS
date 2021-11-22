using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace UniversaLIS
{
     partial class TcpPort : IPortAdapter
     {
          // Please note that UniversaLIS currently supports only one TCP connection per port.
          readonly TcpListener server;
          Socket? client;
          readonly byte[] bytes = new byte[64000];
          readonly StringBuilder incomingData = new StringBuilder();
          private readonly string portName;
          public TcpPort(Tcp tcpSettings)
          {
               int port = tcpSettings.Socket;
               IPAddress localAddr = IPAddress.Parse("127.0.0.1");
               server = new TcpListener(localAddr, port);
               portName = ((IPEndPoint)server.LocalEndpoint).Port.ToString();

          }

          string IPortAdapter.PortName
          {
               get => portName;
          }

          public event EventHandler? PortDataReceived;

          void IPortAdapter.Close()
          {
               if (!(client is null))
               {
                    client.Close();
               }
               server.Stop();
          }

          void IPortAdapter.Open()
          {
               server.Start(1); // Only one instrument connection per port, for simplicity.
               while (true)
               {
                    Console.WriteLine("Waiting for a connection...");
                    client = server.AcceptSocket();
                    AppendToLog("Connected!");
                    incomingData.Clear();
                    client.ReceiveTimeout = 100;
                    while (client.Connected)
                    {
                         int bytesReceived = client.Receive(bytes);
                         incomingData.Append(Encoding.UTF8.GetString(bytes, 0, bytesReceived));
                    }
               }
               //if (!(server is null))
               //{
               //     if (!isActive)
               //     {
               //          server.Start(1);
               //     }
               //     while (true)
               //     {
               //          if (server.Pending())
               //          {
               //               client = server.AcceptSocket();
               //               incomingData = new StringBuilder();
               //               break;
               //          }
               //     }
               //     while (client.Connected)
               //     {

               //          int i;
               //          while ((i = client.Receive(bytes)) != 0)
               //          {
               //               incomingData.Append(Encoding.UTF8.GetString(bytes, 0, i));
               //          }
               //     }
               //}
          }
          string IPortAdapter.ReadChars()
          {
               string buffer = incomingData.ToString();
               incomingData.Clear();
               return buffer;
          }
          string IPortAdapter.PortType()
          {
               return "tcp";
          }

          public void AppendToLog(string txt)
          {
               string? publicFolder = Environment.GetEnvironmentVariable("AllUsersProfile");
               var date = DateTime.Now;
               string txtFile = $"{publicFolder}\\UniversaLIS\\Tcp_Logs\\TcpLog-{portName}_{date.Year}-{date.Month}-{date.Day}.txt";
               if (!Directory.Exists($"{publicFolder}\\UniversaLIS\\Tcp_Logs\\"))
               {
                    Directory.CreateDirectory($"{publicFolder}\\UniversaLIS\\Tcp_Logs\\");
               }
               string txtWrite = $"{date.ToLocalTime()} \t{txt}\r\n";
               File.AppendAllText(txtFile, txtWrite);
          }

          void IPortAdapter.Send(string messageText)
          {
               if (!(client is null))
               {
                    int byteCount = messageText.Length;
                    byte[] sendBytes = Encoding.ASCII.GetBytes(messageText);
                    client.Send(sendBytes, byteCount, SocketFlags.None);
               }
               else
               {
                    throw new ArgumentNullException(nameof(messageText));
               }
          }
     }
}
