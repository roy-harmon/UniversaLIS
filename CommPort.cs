using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace UniversaLIS
{
     public class CommPort :  IPortAdapter //ReliableSerialPort
     {
          private readonly SerialPort serialPort = new SerialPort();
          public CommPort(Serial serial)
          {
               serialPort.PortName = serial.Portname;
               serialPort.BaudRate = serial.Baud;
               serialPort.DataBits = serial.Databits;
               serialPort.Parity = serial.Parity;
               serialPort.StopBits = serial.Stopbits;
               serialPort.Handshake = serial.Handshake;
               serialPort.DataReceived += OnSerialDataReceived;
          }
          public void Send(string messageText)
          {
               AppendToLog($"Out: \t{messageText}");
               byte[] bytes = serialPort.Encoding.GetBytes(messageText);
               for (int i = 0; i < bytes.Length; i++)
               {
                    serialPort.Write(bytes, i, 1);
               }
          }
          public static readonly EventWaitHandle logOpen = new EventWaitHandle(true, EventResetMode.AutoReset);

          string IPortAdapter.PortName {
               get
               {
                    return serialPort.PortName;
               }
          }

          public void AppendToLog(string txt)
          {
               string? publicFolder = Environment.GetEnvironmentVariable("AllUsersProfile");
               var date = DateTime.Now;
               string txtFile = $"{publicFolder}\\UniversaLIS\\Serial_Logs\\SerialLog-{serialPort.PortName}_{date.Year}-{date.Month}-{date.Day}.txt";
               if (!Directory.Exists($"{publicFolder}\\UniversaLIS\\Serial_Logs\\"))
               {
                    Directory.CreateDirectory($"{publicFolder}\\UniversaLIS\\Serial_Logs\\");
               }
               string txtWrite = $"{date.ToLocalTime()} \t{txt}\r\n";
               _ = logOpen.WaitOne();
               File.AppendAllText(txtFile, txtWrite);
               _ = logOpen.Set();
          }

          public event EventHandler? PortDataReceived;

          protected void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs eventArgs)
          {
               EventHandler? handler = PortDataReceived;
               handler?.Invoke(sender, eventArgs);
          }

          public void Open()
          {
               serialPort.Open();
          }

          public void Close()
          {
               serialPort.Close();
          }

          string IPortAdapter.ReadChars()
          {
               char readChar = (char)serialPort.ReadChar();
               return $"{readChar}";
          }

          string IPortAdapter.PortType()
          {
               return "serial";
          }
     }
}