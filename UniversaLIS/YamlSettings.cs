using System.Collections.Generic;

namespace UniversaLIS
{
     public class YamlSettings
     {

          public InterfaceSettings? Interfaces { get; set; }
          public ServiceConfig? ServiceConfig { get; set; }
          public class InterfaceSettings
          {
               public List<Serial>? Serial { get; set; }
               public List<Tcp>? Tcp { get; set; }
          }
          public string? RestLisAddress { get; set; }
     }
     public interface IPortSettings
     {
          public string? ReceiverId { get; set; }
          public string? Password { get; set; }
          public bool UseLegacyFrameSize { get; set; }
          public int AutoSendOrders { get; set; }
          public string GetPortDetails();
     }
     public class Tcp : IPortSettings
     {
          public string? ReceiverId { get; set; }
          public string? Password { get; set; }
          public bool UseLegacyFrameSize { get; set; }
          public int Socket { get; set; }
          public int AutoSendOrders { get; set; }

          public string GetPortDetails()
          {
               return $"{Socket}";
          }
     }
     public class Serial : IPortSettings
     {
          public string? ReceiverId { get; set; }
          public string? Password { get; set; }
          public string? Portname { get; set; }
          public int Baud { get; set; }
          public System.IO.Ports.Parity Parity { get; set; }
          public int Databits { get; set; }
          public System.IO.Ports.StopBits Stopbits { get; set; }
          public System.IO.Ports.Handshake Handshake { get; set; }
          public bool UseLegacyFrameSize { get; set; }
          public int AutoSendOrders { get; set; }
          public string GetPortDetails()
          {
               int stopbits = 0;
               if (Stopbits == System.IO.Ports.StopBits.One)
               {
                    stopbits = 1;
               }
               else if (Stopbits == System.IO.Ports.StopBits.Two)
               {
                    stopbits = 2;
               }
               char parity = 'N';
               if (Parity == System.IO.Ports.Parity.Even)
               {
                    parity = 'E';
               }
               else if (Parity == System.IO.Ports.Parity.Odd)
               {
                    parity = 'O';
               }
               else if (Parity == System.IO.Ports.Parity.Mark)
               {
                    parity = 'M';
               }
               else if (Parity == System.IO.Ports.Parity.Space)
               {
                    parity = 'S';
               }
               return $"{Databits}{parity}{stopbits}";
          }
     }

     public class ServiceConfig
     {
          public string? LisId { get; set; }
          public string? Address { get; set; }
          public string? Phone { get; set; }
     }



}
