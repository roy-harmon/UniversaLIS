using System;
using System.Collections.Generic;

namespace UniversaLIS
{
     public class Order
     {
          public Dictionary<string, string> Elements = new Dictionary<string, string>();

          public int OrderID;

          public List<Result> Results = new List<Result>();

          public string OrderMessage
          {
               get
               {
                    return GetOrderString();
               }
               set
               {
                    SetOrderString(value);
               }
          }

          private string GetOrderString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence#", "SpecimenID", "InstrSpecID", "UniversalTestID", "Priority", "OrderDate", "CollectionDate", "CollectionEndTime", "CollectionVolume", "CollectorID", "ActionCode", "DangerCode", "RelevantClinicInfo", "SpecimenRecvd", "SpecimenDescriptor", "OrderingPhysician", "PhysicianTelNo", "UF1", "UF2", "LF1", "LF2", "LastReported", "BillRef", "InstrSectionID", "ReportType", "Reserved", "SpecCollectLocation", "NosInfFlag", "SpecService", "SpecInstitution" };
               foreach (var item in elementArray)
               {
                    if (!Elements.ContainsKey(item))
                    {
                         Elements.Add(item, "");
                    }
               }
               string output = Constants.STX + Elements["FrameNumber"].Trim('O') + "O|";
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
     }
}