using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     [SwaggerSchema(Required = new[] { "Description" })]
     public class OrderBase
     {
          private protected int orderID;
          private List<Comment> comments = new List<Comment>();
          private OrderedDictionary elements = new OrderedDictionary();
          private List<Result> results = new List<Result>();
          [JsonIgnore, NotMapped]
          public OrderedDictionary Elements { get => elements; set => elements = value; }

          [JsonPropertyOrder(0)]
          [Key]
          public int OrderID { get => orderID; set => orderID = value; }
          [JsonPropertyOrder(1)]
          public string? SpecimenID { get => (string?)Elements["SpecimenID"]; set => Elements["SpecimenID"] = value; }
          [JsonPropertyOrder(2)]
          public string? InstrSpecID { get => (string?)Elements["InstrSpecID"]; set => Elements["InstrSpecID"] = value; }
          [JsonPropertyOrder(3), SwaggerSchema("The test ID field is used to identify a test or battery name. The four parts defined by the " +
               "specification are the universal test identifier, the test name, the test identifier type, and the manufacturer-defined test code, separated by a delimiter (^). " +
               "Some manufacturers only utilize the first part; the resulting UniversalTestID string consists of the test code followed by '^^^' due to the unused parts of the field.")]
          public string UniversalTestID { get => (string)(Elements["UniversalTestID"] ?? "^^^"); set => Elements["UniversalTestID"] = value; }
          [JsonPropertyOrder(4)]
          public string? Priority { get => (string?)Elements["Priority"]; set => Elements["Priority"] = value; }
          [JsonPropertyOrder(5)]
          public string? OrderDate { get => (string?)Elements["OrderDate"]; set => Elements["OrderDate"] = value; }
          [JsonPropertyOrder(6)]
          public string? CollectionDate { get => (string?)Elements["CollectionDate"]; set => Elements["CollectionDate"] = value; }
          [JsonPropertyOrder(7)]
          public string? CollectionEndTime { get => (string?)Elements["CollectionEndTime"]; set => Elements["CollectionEndTime"] = value; }
          [JsonPropertyOrder(8)]
          public string? CollectionVolume { get => (string?)Elements["CollectionVolume"]; set => Elements["CollectionVolume"] = value; }
          [JsonPropertyOrder(9)]
          public string? CollectorID { get => (string?)Elements["CollectorID"]; set => Elements["CollectorID"] = value; }
          [JsonPropertyOrder(10)]
          public string? ActionCode { get => (string?)Elements["ActionCode"]; set => Elements["ActionCode"] = value; }
          [JsonPropertyOrder(11)]
          public string? DangerCode { get => (string?)Elements["DangerCode"]; set => Elements["DangerCode"] = value; }
          [JsonPropertyOrder(12)]
          public string? RelevantClinicInfo { get => (string?)Elements["RelevantClinicInfo"]; set => Elements["RelevantClinicInfo"] = value; }
          [JsonPropertyOrder(13)]
          public string? SpecimenRecvd { get => (string?)Elements["SpecimenRecvd"]; set => Elements["SpecimenRecvd"] = value; }
          [JsonPropertyOrder(14)]
          public string? SpecimenDescriptor { get => (string?)Elements["SpecimenDescriptor"]; set => Elements["SpecimenDescriptor"] = value; }
          [JsonPropertyOrder(15)]
          public string? OrderingPhysician { get => (string?)Elements["OrderingPhysician"]; set => Elements["OrderingPhysician"] = value; }
          [JsonPropertyOrder(16)]
          public string? PhysicianTelNo { get => (string?)Elements["PhysicianTelNo"]; set => Elements["PhysicianTelNo"] = value; }
          [JsonPropertyOrder(17)]
          public string? UF1 { get => (string?)Elements["UF1"]; set => Elements["UF1"] = value; }
          [JsonPropertyOrder(18)]
          public string? UF2 { get => (string?)Elements["UF2"]; set => Elements["UF2"] = value; }
          [JsonPropertyOrder(19)]
          public string? LF1 { get => (string?)Elements["LF1"]; set => Elements["LF1"] = value; }
          [JsonPropertyOrder(20)]
          public string? LF2 { get => (string?)Elements["LF2"]; set => Elements["LF2"] = value; }
          [JsonPropertyOrder(21)]
          public string? LastReported { get => (string?)Elements["LastReported"]; set => Elements["LastReported"] = value; }
          [JsonPropertyOrder(22)]
          public string? BillRef { get => (string?)Elements["BillRef"]; set => Elements["BillRef"] = value; }
          [JsonPropertyOrder(23)]
          public string? InstrSectionID { get => (string?)Elements["InstrSectionID"]; set => Elements["InstrSectionID"] = value; }
          [JsonPropertyOrder(24)]
          public string? ReportType { get => (string?)Elements["ReportType"]; set => Elements["ReportType"] = value; }
          [JsonPropertyOrder(25)]
          public string? Reserved { get => (string?)Elements["Reserved"]; set => Elements["Reserved"] = value; }
          [JsonPropertyOrder(26)]
          public string? SpecCollectLocation { get => (string?)Elements["SpecCollectLocation"]; set => Elements["SpecCollectLocation"] = value; }
          [JsonPropertyOrder(27)]
          public string? NosInfFlag { get => (string?)Elements["NosInfFlag"]; set => Elements["NosInfFlag"] = value; }
          [JsonPropertyOrder(28)]
          public string? SpecService { get => (string?)Elements["SpecService"]; set => Elements["SpecService"] = value; }
          [JsonPropertyOrder(29)]
          public string? SpecInstitution { get => (string?)Elements["SpecInstitution"]; set => Elements["SpecInstitution"] = value; }
          public virtual List<Result> Results { get => results; set => results = value; }
          // TODO: Add support for comment records.
          [JsonIgnore, NotMapped]
          internal List<Comment> Comments { get => comments; set => comments = value; }

          public string GetOrderMessage()
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

          public void SetOrderMessage(string value)
          {
               string[] inArray = value.Split('|');
               if (inArray.Length < 31)
               {
                    // Invalid number of elements.
                    throw new Exception("Invalid number of elements in order record string.");
               }
               Elements["FrameNumber"] = nullifyEmptyString(inArray[0]);
               Elements["Sequence#"] = nullifyEmptyString(inArray[1]);
               Elements["SpecimenID"] = nullifyEmptyString(inArray[2]);
               Elements["InstrSpecID"] = nullifyEmptyString(inArray[3]);
               Elements["UniversalTestID"] = nullifyEmptyString(inArray[4]);
               Elements["Priority"] = nullifyEmptyString(inArray[5]);
               Elements["OrderDate"] = nullifyEmptyString(inArray[6]);
               Elements["CollectionDate"] = nullifyEmptyString(inArray[7]);
               Elements["CollectionEndTime"] = nullifyEmptyString(inArray[8]);
               Elements["CollectionVolume"] = nullifyEmptyString(inArray[9]);
               Elements["CollectorID"] = nullifyEmptyString(inArray[10]);
               Elements["ActionCode"] = nullifyEmptyString(inArray[11]);
               Elements["DangerCode"] = nullifyEmptyString(inArray[12]);
               Elements["RelevantClinicInfo"] = nullifyEmptyString(inArray[13]);
               Elements["SpecimenRecvd"] = nullifyEmptyString(inArray[14]);
               Elements["SpecimenDescriptor"] = nullifyEmptyString(inArray[15]);
               Elements["OrderingPhysician"] = nullifyEmptyString(inArray[16]);
               Elements["PhysicianTelNo"] = nullifyEmptyString(inArray[17]);
               Elements["UF1"] = nullifyEmptyString(inArray[18]);
               Elements["UF2"] = nullifyEmptyString(inArray[19]);
               Elements["LF1"] = nullifyEmptyString(inArray[20]);
               Elements["LF2"] = nullifyEmptyString(inArray[21]);
               Elements["LastReported"] = nullifyEmptyString(inArray[22]);
               Elements["BillRef"] = nullifyEmptyString(inArray[23]);
               Elements["InstrSectionID"] = nullifyEmptyString(inArray[24]);
               Elements["ReportType"] = nullifyEmptyString(inArray[25]);
               Elements["Reserved"] = nullifyEmptyString(inArray[26]);
               Elements["SpecCollectLocation"] = nullifyEmptyString(inArray[27]);
               Elements["NosInfFlag"] = nullifyEmptyString(inArray[28]);
               Elements["SpecService"] = nullifyEmptyString(inArray[29]);
               Elements["SpecInstitution"] = nullifyEmptyString(inArray[30]);
          }

          private static string? nullifyEmptyString(string? input)
          {
               if (input == "") {
                    return null;
               }
               return input;
          }
     }
}