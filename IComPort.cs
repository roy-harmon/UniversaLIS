using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversaLIS
{
     interface IComPort
     {
          public void Send();
          public void Open();
          public void Close();
          //public void Write(byte[] buffer, int offset, int count);
          //public int Read(byte[] buffer, int offset, int count);
     }
}
