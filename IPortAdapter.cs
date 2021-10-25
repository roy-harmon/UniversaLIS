using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversaLIS
{
     interface IPortAdapter
     {
          public string PortName { get; }
          public string PortType();
          public void Send(string messageText);
          public void Open();
          public void Close();
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
          //public void Write(byte[] buffer, int offset, int count);
          //public int Read(byte[] buffer, int offset, int count);
     }
}
