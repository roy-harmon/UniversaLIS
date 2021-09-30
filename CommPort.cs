using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace UniversaLIS
{
     public class CommPort : SerialPort //ReliableSerialPort
     {
          public CommPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) : base(portName, baudRate, parity, dataBits, stopBits)
          {
          }

          public void Send(string messageText)
          {
               AppendToLog($"Out: \t{messageText}");
               byte[] bytes = Encoding.GetBytes(messageText);
               for (int i = 0; i < bytes.Length; i++)
               {
                    Write(bytes, i, 1);
               }
          }
          public static readonly EventWaitHandle logOpen = new EventWaitHandle(true, EventResetMode.AutoReset);
          public static void AppendToLog(string txt)
          {
               string publicFolder = Environment.GetEnvironmentVariable("AllUsersProfile");
               var date = DateTime.Now;
               string txtFile = $"{publicFolder}\\UniversaLIS\\Serial_Logs\\SerialLog_{date.Year}-{date.Month}-{date.Day}.txt";
               if (Directory.Exists($"{publicFolder}\\UniversaLIS\\Serial_Logs\\") == false)
               {
                    Directory.CreateDirectory($"{publicFolder}\\UniversaLIS\\Serial_Logs\\");
               }
               string txtWrite = $"{date.ToLocalTime()} \t{txt}\r\n";
               _ = logOpen.WaitOne();
               File.AppendAllText(txtFile, txtWrite);
               _ = logOpen.Set();
          }
     }
}