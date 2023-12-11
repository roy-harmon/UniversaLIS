using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     [Table("OrderRecord")]
     public class Order : OrderBase
     {
          private protected int patientID;
          private List<Result> results = new();
          [JsonIgnore, ForeignKey(nameof(PatientID)), InverseProperty("Orders")]
          public Patient Patient { get; set; }
          [JsonIgnore]
          public int PatientID { get => Patient.PatientID; set => Patient.PatientID = value; }
          [JsonPropertyOrder(100), SwaggerSchema("The list of results associated with this order.")]
          public override List<Result> Results { get => results; set => results = value; }
          public Order(string orderMessage, Patient patient)
          {
               SetOrderString(orderMessage);
               Patient = patient;
          }

          public Order(string orderMessage, int orderID)
          {
               SetOrderString(orderMessage);
               OrderID = orderID;
               Patient = new Patient();
          }

          public Order(Patient patient)
          {
               SetOrderMessage("O||||^^^||||||||||||||||||||||||||");
               Patient = patient;
          }

          public Order()
          {
               SetOrderMessage("O||||^^^||||||||||||||||||||||||||");
               Patient = new Patient();
          }

          private string GetOrderString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence#", "SpecimenID", "InstrSpecID", "UniversalTestID", "Priority", "OrderDate", "CollectionDate", "CollectionEndTime", "CollectionVolume", "CollectorID", "ActionCode", "DangerCode", "RelevantClinicInfo", "SpecimenRecvd", "SpecimenDescriptor", "OrderingPhysician", "PhysicianTelNo", "UF1", "UF2", "LF1", "LF2", "LastReported", "BillRef", "InstrSectionID", "ReportType", "Reserved", "SpecCollectLocation", "NosInfFlag", "SpecService", "SpecInstitution" };
               foreach (var item in elementArray)
               {
                    if (!Elements.Contains(item))
                    {
                         Elements.Add(item, "");
                    }
               }
               string output = Constants.STX + $"{Elements["FrameNumber"]}".Trim('O') + "O|";
               // Concatenate the Dictionary values and return the string.
               output += Elements["Sequence#"] + "|";
               output += Elements["SpecimenID"] + "|";
               output += Elements["InstrSpecID"] + "|";
               output += Elements["UniversalTestID"] + "|";
               output += Elements["Priority"] + "|";
               output += Elements["OrderDate"] + "|";
               output += Elements["CollectionDate"] + "|";
               output += Elements["CollectionEndTime"] + "|";
               output += Elements["CollectionVolume"] + "|";
               output += Elements["CollectorID"] + "|";
               output += Elements["ActionCode"] + "|";
               output += Elements["DangerCode"] + "|";
               output += Elements["RelevantClinicInfo"] + "|";
               output += Elements["SpecimenRecvd"] + "|";
               output += Elements["SpecimenDescriptor"] + "|";
               output += Elements["OrderingPhysician"] + "|";
               output += Elements["PhysicianTelNo"] + "|";
               output += Elements["UF1"] + "|";
               output += Elements["UF2"] + "|";
               output += Elements["LF1"] + "|";
               output += Elements["LF2"] + "|";
               output += Elements["LastReported"] + "|";
               output += Elements["BillRef"] + "|";
               output += Elements["InstrSectionID"] + "|";
               output += Elements["ReportType"] + "|";
               output += Elements["Reserved"] + "|";
               output += Elements["SpecCollectLocation"] + "|";
               output += Elements["NosInfFlag"] + "|";
               output += Elements["SpecService"] + "|";
               output += Elements["SpecInstitution"] + Constants.CR + Constants.ETX;
               return output;
          }

          private void SetOrderString(string input)
          {
               string[] inArray = input.Split('|');
               if (inArray.Length < 31)
               {
                    // Invalid number of elements.
                    throw new Exception("Invalid number of elements in order record string.");
               }
               Elements["FrameNumber"] = inArray[0];
               Elements["Sequence#"] = inArray[1];
               Elements["SpecimenID"] = inArray[2];
               Elements["InstrSpecID"] = inArray[3];
               Elements["UniversalTestID"] = inArray[4];
               Elements["Priority"] = inArray[5];
               Elements["OrderDate"] = inArray[6];
               Elements["CollectionDate"] = inArray[7];
               Elements["CollectionEndTime"] = inArray[8];
               Elements["CollectionVolume"] = inArray[9];
               Elements["CollectorID"] = inArray[10];
               Elements["ActionCode"] = inArray[11];
               Elements["DangerCode"] = inArray[12];
               Elements["RelevantClinicInfo"] = inArray[13];
               Elements["SpecimenRecvd"] = inArray[14];
               Elements["SpecimenDescriptor"] = inArray[15];
               Elements["OrderingPhysician"] = inArray[16];
               Elements["PhysicianTelNo"] = inArray[17];
               Elements["UF1"] = inArray[18];
               Elements["UF2"] = inArray[19];
               Elements["LF1"] = inArray[20];
               Elements["LF2"] = inArray[21];
               Elements["LastReported"] = inArray[22];
               Elements["BillRef"] = inArray[23];
               Elements["InstrSectionID"] = inArray[24];
               Elements["ReportType"] = inArray[25];
               Elements["Reserved"] = inArray[26];
               Elements["SpecCollectLocation"] = inArray[27];
               Elements["NosInfFlag"] = inArray[28];
               Elements["SpecService"] = inArray[29];
               Elements["SpecInstitution"] = inArray[30];
          }

     }
}