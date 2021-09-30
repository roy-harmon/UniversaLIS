using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;
using YamlDotNet.RepresentationModel;
// TODO: Add internal database while keeping external database option.
// TODO: Add UI.
namespace UniversaLIS
{
     public partial class ServiceMain : ServiceBase
     {
          public static EventLog EventLog1 = new EventLog();
          private static readonly List<CommFacilitator> s_commFacilitators = new List<CommFacilitator>();
          public bool ListenHL7;
          public int HL7Port;
          public bool UseExtDB;
          public string ExternalDbConnString;
          public int DbPollInterval;
          public ServiceMain()
          {
               InitializeComponent();
               if (!EventLog.SourceExists("UniversaLIS"))
               {
                    EventLog.CreateEventSource(
                        "UniversaLIS", "IMMULog");
               }
               EventLog1.Source = "UniversaLIS";
               EventLog1.Log = "IMMULog";
          }

          public static void HandleEx(Exception ex)
          {
               if (ex is null)
               {
                    return;
               }
               string message = ex.Source + " - Error: " + ex.Message + "\n" + ex.TargetSite + "\n" + ex.StackTrace;
               EventLog1.WriteEntry(message);
          }

          protected override void OnStart(string[] args)
          {
               try
               {
                    AppendToLog("Service starting.");
                    using (var reader = new StreamReader("Properties/config.yml"))
                    {
                         var yaml = new YamlStream();
                         yaml.Load(reader);
                         var yamlmap = (YamlMappingNode)yaml.Documents[0].RootNode;
                         // Set up database connections, HL7 listener, and RS232 interfaces.
                         var serviceConfig = (YamlMappingNode)yamlmap.Children[new YamlMappingNode("service_config")];
                         var listenNode = (YamlScalarNode)serviceConfig.Children[new YamlScalarNode("listen_hl7")];
                         ListenHL7 = bool.Parse(listenNode.Value);
                         var useExtDbNode = (YamlScalarNode)serviceConfig.Children[new YamlScalarNode("use_external_db")];
                         UseExtDB = bool.Parse(useExtDbNode.Value);
                         if (UseExtDB)
                         {
                              var extDbConnNode = (YamlScalarNode)serviceConfig.Children[new YamlScalarNode("listen_hl7")];
                              ExternalDbConnString = extDbConnNode.Value;
                              // TODO: Map internal DB fields to external ones.
                         }
                         if (ListenHL7)
                         {
                              var listenPortNode = (YamlScalarNode)serviceConfig.Children[new YamlScalarNode("listen_hl7")];
                              HL7Port = int.Parse(listenPortNode.Value);
                              // TODO: Actually set up the HL7 listener.
                         }
                         var interfaces = (YamlSequenceNode)yamlmap.Children[new YamlScalarNode("interfaces")];
                         foreach (YamlMappingNode iface in interfaces)
                         {
                              string baud = $"{iface.Children[new YamlScalarNode("baud")]}";
                              string dbits = $"{iface.Children[new YamlScalarNode("databits")]}";
                              string sbits = $"{iface.Children[new YamlScalarNode("stopbits")]}";
                              string hshake = $"{iface.Children[new YamlScalarNode("handshake")]}";
                              string rec_id = $"{iface.Children[new YamlScalarNode("receiver_id")]}";
                              s_commFacilitators.Add(new CommFacilitator((string)iface.Children[new YamlScalarNode("portname")], int.Parse(baud), (string)iface.Children[new YamlScalarNode("parity")], int.Parse(dbits), sbits, hshake, rec_id));
                         }
                    }
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
          }
          protected override void OnStop()
          {
               
               try
               {
                    AppendToLog("Service stopping.");
                    foreach (var comm in s_commFacilitators)
                    {
                         comm.Close();
                    }
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
          }

          public static void AppendToLog(string txt)
          {    // To avoid write conflicts, let the CommPort handle the actual data logging.
               CommPort.AppendToLog(txt);
          }


          public static string CHKSum(string message)
          {
               // This function returns the checksum for the data string passed to it.
               // If I've done it right, the checksum is calculated by binary 8-bit addition of all included characters
               // with the 8th or parity bit assumed zero. Carries beyond the 8th bit are lost. The 8-bit result is
               // converted into two printable ASCII Hex characters ranging from 00 to FF, which are then inserted into
               // the data stream. Hex alpha characters are always uppercase.

               string checkSum;
               int ascSum, modVal;
               ascSum = 0;
               foreach (char c in message)
               {
                    if ((int)c != 2)
                    {    // Don't count any STX.
                         ascSum += (int)c;
                    }
               }
               modVal = ascSum % 256;
               checkSum = modVal.ToString("X");
               return checkSum.PadLeft(2, '0');
          }

          // <summary>Method invoked when service is started from a debugging console.</summary>
          internal void DebuggingRoutine(string[] args)
          {
               OnStart(args);
               Console.ReadLine();
               OnStop();
          }
     }

}