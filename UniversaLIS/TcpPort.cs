using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UniversaLIS
{
     
     partial class TcpPort : IPortAdapter
     {
          private const int BUFFER_SIZE = 64000;

          // Please note that UniversaLIS currently supports only one TCP connection per port.
          readonly TcpListener server;
          Socket? client;
          readonly byte[] readBuffer = new byte[BUFFER_SIZE];
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

          private readonly System.Timers.Timer portTimer = new System.Timers.Timer();

          /* This procedure may or may not evolve into something useful. */
          protected void CheckDataReceived()
          {
               bool timedOut = false;
               if (!(client is null) && client.Connected)
               {
                    while (!timedOut)
                    {
                         int bytesReceived = 0;
                         try
                         {
                              bytesReceived = client.Receive(readBuffer);
                              incomingData.Append(Encoding.UTF8.GetString(readBuffer, 0, bytesReceived));

                         }
                         catch (SocketException)
                         {
                              // Most likely a timeout. Ignore it.
                              timedOut = true;
                         }
                         if (bytesReceived == 0)
                         {
                              timedOut = true;
                         }
                    }
                    if (incomingData.Length > 0)
                    {
                         EventHandler? handler = this.PortDataReceived;
                         EventArgs eventArgs = new EventArgs();
                         handler?.Invoke(this, eventArgs);
                    }
               }

          }
          private void CheckDataReceived(Object? source, System.Timers.ElapsedEventArgs e)
          {
               portTimer.Stop();
               CheckDataReceived();
               portTimer.Start();
          }

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
               Console.WriteLine("Waiting for a connection...");
               client = server.AcceptSocket();
               AppendToLog("Connected!");
               incomingData.Clear();
               client.ReceiveTimeout = 100;
               portTimer.Interval = 1000;
               portTimer.Elapsed += CheckDataReceived;
               portTimer.AutoReset = true;
               portTimer.Start();
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
                    byte[] sendBytes = Encoding.ASCII.GetBytes(messageText);
                    client.Send(sendBytes);
               }
               else
               {
                    throw new ArgumentNullException(nameof(messageText));
               }
          }
     }
}
