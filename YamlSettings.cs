using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversaLIS
{
     public class YamlSettings
     {
          
          public InterfaceSettings Interfaces;
          public ServiceConfig ServiceConfig;
          public TableMappings TableFieldMappings;
          public class InterfaceSettings
          {
               public List<Serial> Serial { get; set; }
               public List<Tcp> Tcp { get; set; }
                              
          }
     }
     public class Tcp
     {
          public string ReceiverId { get; set; }
          public string Password { get; set; }
          public bool UseLegacyFrameSize { get; set; }
          public int Socket { get; set; }
          public int AutoSendOrders;
     }
     public class Serial
     {
          public string ReceiverId { get; set; }
          public string Password { get; set; }
          public string Portname { get; set; }
          public int Baud { get; set; }
          public System.IO.Ports.Parity Parity { get; set; }
          public int Databits { get; set; }
          public System.IO.Ports.StopBits Stopbits { get; set; }
          public System.IO.Ports.Handshake Handshake { get; set; }
          public bool UseLegacyFrameSize { get; set; }
          public int AutoSendOrders;
          //(Parity)Enum.Parse(typeof(Parity), parity, true), databits, (StopBits)Enum.Parse(typeof(StopBits), stopbits, true)
     }
     public class TableMappings
     {
          // TODO: Add methods somewhere for mapping internal database fields to external database fields.
          public PatientRecordSet PatientRecord;
          public OrderRecordSet OrderRecord;
          public ResultRecordSet ResultRecord;
          public QueryRecordSet QueryRecord;
          public CommentRecordSet CommentRecord;
          public ScientificRecordSet ScientificRecord;


          public class PatientRecordSet
          {
               public string PAPID;
               public string LAPID;
               public string PID3;
               public string PName;
               public string MMName;
               public string DOB;
               public string PSex;
               public string PRace;
               public string PAddr;
               public string Reserved;
               public string PTelNo;
               public string Attending;
               public string Special1;
               public string Special2;
               public string PHeight;
               public string PWeight;
               public string PDiag;
               public string PMeds;
               public string PDiet;
               public string PF1;
               public string PF2;
               public string AdmDates;
               public string AdmStatus;
               public string PLocation;
               public string AltCodeNature;
               public string AltCode;
               public string PReligion;
               public string PMarStatus;
               public string PIsoStatus;
               public string PLanguage;
               public string HospService;
               public string HospInst;
               public string DoseCat;
          }

          public class OrderRecordSet
          {
               public string PRID;
               public string SpecID;
               public string InSpecID;
               public string UTID;
               public string OrderDate;
               public string CollectDate;
               public string CollEndTime;
               public string CollVolume;
               public string Collector;
               public string ActCode;
               public string DangerCode;
               public string RelClinInfo;
               public string SpecRecd;
               public string SpecDesc;
               public string OrdPhys;
               public string PhysTelNo;
               public string UF1;
               public string UF2;
               public string LF1;
               public string LF2;
               public string LastReported;
               public string BillRef;
               public string InSectID;
               public string RepType;
               public string Reserved;
               public string SpecColLocation;
               public string NosInfFlag;
               public string SpecService;
               public string SpecInst;

          }

          public class ResultRecordSet
          {
               public string ORID;
               public string UTID;
               public string Result;
               public string Unit;
               public string RefRange;
               public string Abnormal;
               public string AbNature;
               public string ResStatus;
               public string NormsChanged;
               public string OpID;
               public string TestStart;
               public string TestEnd;
               public string InstID;

          }

          public class QueryRecordSet
          {
               public string StartRange;
               public string EndRange;
               public string UTID;
               public string ReqLimNature;
               public string ReqResBeginDT;
               public string ReqResEndDT;
               public string ReqPhysName;
               public string ReqPhysTelNo;
               public string UF1;
               public string UF2;
               public string ReqInfoStatus;

          }

          public class CommentRecordSet
          {
               public string Source;
               public string Text;
               public string Type;

          }

          public class ScientificRecordSet
          {
               public string AnalMeth;
               public string Instrument;
               public string Reagents;
               public string Units;
               public string QC;
               public string SpecDesc;
               public string Reserved;
               public string Container;
               public string SpecID;
               public string Analyte;
               public string Result;
               public string ResUnits;
               public string CollectDT;
               public string ResultDT;
               public string AnalPreSteps;
               public string PatDiag;
               public string PatDOB;
               public string PSex;
               public string PRace;
          }
     }

     public class ServiceConfig
     {
          public bool UseExternalDb;
          public string ConnectionString;
          public int DbPollInterval;
          public bool ListenHl7;
          public int Hl7TcpPort;
          public string LisId;
          public string Address;
          public string Phone;
     }



}
