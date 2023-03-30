using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace UniversaLIS.Models
{
     public class Order
     {
          [NotMapped]
          [JsonIgnore]
          public OrderedDictionary Elements = new OrderedDictionary();

          private protected int orderID;

          private List<Result> results = new List<Result>();
          private List<Comment> comments = new List<Comment>();

          public string GetOrderMessage()
          {
               return GetOrderString();
          }

          public void SetOrderMessage(string value)
          {
               SetOrderString(value);
          }

          [Key]
          public int OrderID { get => orderID; set => orderID = value; }
          internal List<Comment> Comments { get => comments; set => comments = value; }
          public string? SpecimenID { get => $"{Elements["SpecimenID"]}"; set => Elements["SpecimenID"] = value ?? ""; }
          public string? InstrSpecID { get => $"{Elements["InstrSpecID"]}"; set => Elements["InstrSpecID"] = value ?? ""; }
          public string? UniversalTestID { get => $"{Elements["UniversalTestID"]}"; set => Elements["UniversalTestID"] = value ?? ""; }
          public string? Priority { get => $"{Elements["Priority"]}"; set => Elements["Priority"] = value ?? ""; }
          public string? OrderDate { get => $"{Elements["OrderDate"]}"; set => Elements["OrderDate"] = value ?? ""; }
          public string? CollectionDate { get => $"{Elements["CollectionDate"]}"; set => Elements["CollectionDate"] = value ?? ""; }
          public string? CollectionEndTime { get => $"{Elements["CollectionEndTime"]}"; set => Elements["CollectionEndTime"] = value ?? ""; }
          public string? CollectionVolume { get => $"{Elements["CollectionVolume"]}"; set => Elements["CollectionVolume"] = value ?? ""; }
          public string? CollectorID { get => $"{Elements["CollectorID"]}"; set => Elements["CollectorID"] = value ?? ""; }
          public string? ActionCode { get => $"{Elements["ActionCode"]}"; set => Elements["ActionCode"] = value ?? ""; }
          public string? DangerCode { get => $"{Elements["DangerCode"]}"; set => Elements["DangerCode"] = value ?? ""; }
          public string? RelevantClinicInfo { get => $"{Elements["RelevantClinicInfo"]}"; set => Elements["RelevantClinicInfo"] = value ?? ""; }
          public string? SpecimenRecvd { get => $"{Elements["SpecimenRecvd"]}"; set => Elements["SpecimenRecvd"] = value ?? ""; }
          public string? SpecimenDescriptor { get => $"{Elements["SpecimenDescriptor"]}"; set => Elements["SpecimenDescriptor"] = value ?? ""; }
          public string? OrderingPhysician { get => $"{Elements["OrderingPhysician"]}"; set => Elements["OrderingPhysician"] = value ?? ""; }
          public string? PhysicianTelNo { get => $"{Elements["PhysicianTelNo"]}"; set => Elements["PhysicianTelNo"] = value ?? ""; }
          public string? UF1 { get => $"{Elements["UF1"]}"; set => Elements["UF1"] = value ?? ""; }
          public string? UF2 { get => $"{Elements["UF2"]}"; set => Elements["UF2"] = value ?? ""; }
          public string? LF1 { get => $"{Elements["LF1"]}"; set => Elements["LF1"] = value ?? ""; }
          public string? LF2 { get => $"{Elements["LF2"]}"; set => Elements["LF2"] = value ?? ""; }
          public string? LastReported { get => $"{Elements["LastReported"]}"; set => Elements["LastReported"] = value ?? ""; }
          public string? BillRef { get => $"{Elements["BillRef"]}"; set => Elements["BillRef"] = value ?? ""; }
          public string? InstrSectionID { get => $"{Elements["InstrSectionID"]}"; set => Elements["InstrSectionID"] = value ?? ""; }
          public string? ReportType { get => $"{Elements["ReportType"]}"; set => Elements["ReportType"] = value ?? ""; }
          public string? Reserved { get => $"{Elements["Reserved"]}"; set => Elements["Reserved"] = value ?? ""; }
          public string? SpecCollectLocation { get => $"{Elements["SpecCollectLocation"]}"; set => Elements["SpecCollectLocation"] = value ?? ""; }
          public string? NosInfFlag { get => $"{Elements["NosInfFlag"]}"; set => Elements["NosInfFlag"] = value ?? ""; }
          public string? SpecService { get => $"{Elements["SpecService"]}"; set => Elements["SpecService"] = value ?? ""; }
          public string? SpecInstitution { get => $"{Elements["SpecInstitution"]}"; set => Elements["SpecInstitution"] = value ?? ""; }
          public List<Result> Results { get => results; set => results = value; }

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

          public Order(string orderMessage)
          {
               SetOrderString(orderMessage);
          }

          public Order(string orderMessage, int orderID)
          {
               SetOrderString(orderMessage);
               OrderID = orderID;
          }

          public Order()
          {
               SetOrderString("O||||||||||||||||||||||||||||||");
          }

          public string GetJsonString()
          {
               OrderedDictionary fieldList = new OrderedDictionary();
               IDictionaryEnumerator enumerator = Elements.GetEnumerator();
               while (enumerator.MoveNext())
               {
                    switch (enumerator.Key)
                    {
                         case "FrameNumber":
                         case "Sequence#":
                              break;
                         default:
                              fieldList[enumerator.Key] = enumerator.Value;
                              break;
                    }
               }
               fieldList.Add("Comments", comments);
               fieldList.Add("Results", results);
               return JsonSerializer.Serialize(fieldList);
          }
     }
}