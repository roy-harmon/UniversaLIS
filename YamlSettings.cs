using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversaLIS
{
     public class YamlSettings
     {
          
          public InterfaceSettings? Interfaces { get; set; }
          public ServiceConfig? ServiceConfig { get; set; }
          public TableMappings? TableFieldMappings { get; set; }
          public class InterfaceSettings
          {
               public List<Serial>? Serial { get; set; }
               public List<Tcp>? Tcp { get; set; }
                              
          }
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
          //(Parity)Enum.Parse(typeof(Parity), parity, true), databits, (StopBits)Enum.Parse(typeof(StopBits), stopbits, true)
     }
     public class TableMappings
     {
          public PatientRecordSet? PatientRecord { get; set; }
          public OrderRecordSet? OrderRecord { get; set; }
          public ResultRecordSet? ResultRecord { get; set; }
          public QueryRecordSet? QueryRecord { get; set; }
          public CommentRecordSet? CommentRecord { get; set; }
          public ScientificRecordSet? ScientificRecord { get; set; }

          public class PatientRecordSet
          {
               public string? PAPID { get; set; }
               public string? LAPID { get; set; }
               public string? PID3 { get; set; }
               public string? PName { get; set; }
               public string? MMName { get; set; }
               public string? DOB { get; set; }
               public string? PSex { get; set; }
               public string? PRace { get; set; }
               public string? PAddr { get; set; }
               public string? Reserved { get; set; }
               public string? PTelNo { get; set; }
               public string? Attending { get; set; }
               public string? Special1 { get; set; }
               public string? Special2 { get; set; }
               public string? PHeight { get; set; }
               public string? PWeight { get; set; }
               public string? PDiag { get; set; }
               public string? PMeds { get; set; }
               public string? PDiet { get; set; }
               public string? PF1 { get; set; }
               public string? PF2 { get; set; }
               public string? AdmDates { get; set; }
               public string? AdmStatus { get; set; }
               public string? PLocation { get; set; }
               public string? AltCodeNature { get; set; }
               public string? AltCode { get; set; }
               public string? PReligion { get; set; }
               public string? PMarStatus { get; set; }
               public string? PIsoStatus { get; set; }
               public string? PLanguage { get; set; }
               public string? HospService { get; set; }
               public string? HospInst { get; set; }
               public string? DoseCat { get; set; }

          }

          public class OrderRecordSet
          {
               public string? PRID { get; set; }
               public string? SpecID { get; set; }
               public string? InSpecID { get; set; }
               public string? UTID { get; set; }
               public string? OrderDate { get; set; }
               public string? CollectDate { get; set; }
               public string? CollEndTime { get; set; }
               public string? CollVolume { get; set; }
               public string? Collector { get; set; }
               public string? ActCode { get; set; }
               public string? DangerCode { get; set; }
               public string? RelClinInfo { get; set; }
               public string? SpecRecd { get; set; }
               public string? SpecDesc { get; set; }
               public string? OrdPhys { get; set; }
               public string? PhysTelNo { get; set; }
               public string? UF1 { get; set; }
               public string? UF2 { get; set; }
               public string? LF1 { get; set; }
               public string? LF2 { get; set; }
               public string? LastReported { get; set; }
               public string? BillRef { get; set; }
               public string? InSectID { get; set; }
               public string? RepType { get; set; }
               public string? Reserved { get; set; }
               public string? SpecColLocation { get; set; }
               public string? NosInfFlag { get; set; }
               public string? SpecService { get; set; }
               public string? SpecInst { get; set; }

          }

          public class ResultRecordSet
          {
               public string? ORID { get; set; }
               public string? UTID { get; set; }
               public string? Result { get; set; }
               public string? Unit { get; set; }
               public string? RefRange { get; set; }
               public string? Abnormal { get; set; }
               public string? AbNature { get; set; }
               public string? ResStatus { get; set; }
               public string? NormsChanged { get; set; }
               public string? OpID { get; set; }
               public string? TestStart { get; set; }
               public string? TestEnd { get; set; }
               public string? InstID { get; set; }

          }

          public class QueryRecordSet
          {
               public string? StartRange { get; set; }
               public string? EndRange { get; set; }
               public string? UTID { get; set; }
               public string? ReqLimNature { get; set; }
               public string? ReqResBeginDT { get; set; }
               public string? ReqResEndDT { get; set; }
               public string? ReqPhysName { get; set; }
               public string? ReqPhysTelNo { get; set; }
               public string? UF1 { get; set; }
               public string? UF2 { get; set; }
               public string? ReqInfoStatus { get; set; }

          }

          public class CommentRecordSet
          {
               public string? Source { get; set; }
               public string? Text { get; set; }
               public string? Type { get; set; }

          }

          public class ScientificRecordSet
          {
               public string? AnalMeth { get; set; }
               public string? Instrument { get; set; }
               public string? Reagents { get; set; }
               public string? Units { get; set; }
               public string? QC { get; set; }
               public string? SpecDesc { get; set; }
               public string? Reserved { get; set; }
               public string? Container { get; set; }
               public string? SpecID { get; set; }
               public string? Analyte { get; set; }
               public string? Result { get; set; }
               public string? ResUnits { get; set; }
               public string? CollectDT { get; set; }
               public string? ResultDT { get; set; }
               public string? AnalPreSteps { get; set; }
               public string? PatDiag { get; set; }
               public string? PatDOB { get; set; }
               public string? PSex { get; set; }
               public string? PRace { get; set; }
          }
     }

     public class ServiceConfig
     {
          public bool UseExternalDb { get; set; }
          public string? ConnectionString { get; set; }
          public string? SqlitePath { get; set; }
          public int DbPollInterval { get; set; }
          public bool ListenHl7 { get; set; }
          public int Hl7TcpPort { get; set; }
          public string? LisId { get; set; }
          public string? Address { get; set; }
          public string? Phone { get; set; }
     }



}
