using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace UniversaLIS
{
     
     public class CommPort : IPortAdapter
     {
          private readonly SerialPort serialPort = new SerialPort();
          private readonly CommFacilitator facilitator;
          public CommPort(Serial serial, CommFacilitator facilitator)
          {
               serialPort.PortName = serial.Portname!;
               serialPort.BaudRate = serial.Baud;
               serialPort.DataBits = serial.Databits;
               serialPort.Parity = serial.Parity;
               serialPort.StopBits = serial.Stopbits;
               serialPort.Handshake = serial.Handshake;
               serialPort.DataReceived += OnSerialDataReceived;
               serialPort.ReadTimeout = 50;
               this.facilitator = facilitator;
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

          string IPortAdapter.PortName
          {
               get
               {
                    return serialPort.PortName;
               }
          }

          public void AppendToLog(string txt)
          {
               string? publicFolder = Environment.GetEnvironmentVariable("AllUsersProfile");
               var date = DateTime.Now;
               string txtFile = string.Format($"{publicFolder}\\UniversaLIS\\Serial_Logs\\SerialLog-{serialPort.PortName}_{0}-{1}-{2}.txt",
                    date.Year, $"{date.Month}".PadLeft(2, '0'), date.Day);
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
               handler?.Invoke(this, eventArgs);
          }

          public void Open()
          {
               serialPort.Open();
          }

          public void Close()
          {
               serialPort.Close();
          }

          string GetCharString()
          {
               char readChar = (char)serialPort.ReadChar();
               return $"{readChar}";
          }

          string IPortAdapter.ReadChars()
          {
               System.Text.StringBuilder buffer = new System.Text.StringBuilder();
               bool timedOut = false;
               try
               {
                    /* There are a few messages that won't end in a NewLine,
                     * so we have to read one character at a time until we run out of them.
                     */
                    do
                    { // Read one char at a time until the ReadChar times out.
                         try
                         {
                              buffer.Append(GetCharString());
                         }
                         catch (Exception)
                         {
                              timedOut = true;
                         }
                    } while (!timedOut);
               }
               catch (Exception ex)
               {
                    facilitator.service.HandleEx(ex);
                    throw;
               }
               return buffer.ToString();
          }
          string IPortAdapter.PortType()
          {
               return "serial";
          }
     }
}