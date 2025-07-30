using System;

namespace UniversaLIS
{
     internal interface IPortAdapter
     {
          string PortName { get; }
          string PortType();
          void Send(string messageText);
          void Open();
          void Close();
          internal string ReadChars();
          void AppendToLog(string txt);

          virtual event EventHandler PortDataReceived
          {
               add
               {
                    PortDataReceived += value;
               }
               remove
               {
                    PortDataReceived -= value;
               }
          }
     }
}
