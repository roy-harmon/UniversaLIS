using System;

namespace UniversaLIS
{
     interface IPortAdapter
     {
          public string PortName { get; }
          public string PortType();
          public void Send(string messageText);
          public void Open();
          public void Close();
          internal string ReadChars();
          public virtual event EventHandler PortDataReceived
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
