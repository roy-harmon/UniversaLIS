using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMMULIS
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
               string[] elementArray = { "FrameNumber", "Sequence#", "Specimen ID (Accession#)", "Instrument Specimen ID", "Universal Test ID", "Priority", "Order Date/Time", "Collection Date/Time", "Collection End Time", "Collection Volume", "Collector ID", "Action Code", "Danger Code", "Relevant Clinical Info", "Date/Time Specimen Received", "Specimen Descriptor", "Ordering Physician", "Physician's Telephone Number", "User Field No.1", "User Field No.2", "Lab Field No.1", "Lab Field No.2", "Date/Time results reported or last modified", "Instrument Charge to Computer System", "Instrument Section ID", "Report Types", "Reserved Field", "Location or ward of Specimen Collection", "Nosocomial Infection Flag", "Specimen Service", "Specimen Institution" };
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
               output += Elements["Specimen ID (Accession#)"] + "|";
               output += Elements["Instrument Specimen ID"] + "|";
               output += Elements["Universal Test ID"] + "|";
               output += Elements["Priority"] + "|";
               output += Elements["Order Date/Time"] + "|";
               output += Elements["Collection Date/Time"] + "|";
               output += Elements["Collection End Time"] + "|";
               output += Elements["Collection Volume"] + "|";
               output += Elements["Collector ID"] + "|";
               output += Elements["Action Code"] + "|";
               output += Elements["Danger Code"] + "|";
               output += Elements["Relevant Clinical Info"] + "|";
               output += Elements["Date/Time Specimen Received"] + "|";
               output += Elements["Specimen Descriptor"] + "|";
               output += Elements["Ordering Physician"] + "|";
               output += Elements["Physician's Telephone Number"] + "|";
               output += Elements["User Field No.1"] + "|";
               output += Elements["User Field No.2"] + "|";
               output += Elements["Lab Field No.1"] + "|";
               output += Elements["Lab Field No.2"] + "|";
               output += Elements["Date/Time results reported or last modified"] + "|";
               output += Elements["Instrument Charge to Computer System"] + "|";
               output += Elements["Instrument Section ID"] + "|";
               output += Elements["Report Types"] + "|";
               output += Elements["Reserved Field"] + "|";
               output += Elements["Location or ward of Specimen Collection"] + "|";
               output += Elements["Nosocomial Infection Flag"] + "|";
               output += Elements["Specimen Service"] + "|";
               output += Elements["Specimen Institution"] + Constants.CR + Constants.ETX;
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
               Elements["Specimen ID (Accession#)"] = inArray[2];
               Elements["Instrument Specimen ID"] = inArray[3];
               Elements["Universal Test ID"] = inArray[4];
               Elements["Priority"] = inArray[5];
               Elements["Order Date/Time"] = inArray[6];
               Elements["Collection Date/Time"] = inArray[7];
               Elements["Collection End Time"] = inArray[8];
               Elements["Collection Volume"] = inArray[9];
               Elements["Collector ID"] = inArray[10];
               Elements["Action Code"] = inArray[11];
               Elements["Danger Code"] = inArray[12];
               Elements["Relevant Clinical Info"] = inArray[13];
               Elements["Date/Time Specimen Received"] = inArray[14];
               Elements["Specimen Descriptor"] = inArray[15];
               Elements["Ordering Physician"] = inArray[16];
               Elements["Physician's Telephone Number"] = inArray[17];
               Elements["User Field No.1"] = inArray[18];
               Elements["User Field No.2"] = inArray[19];
               Elements["Lab Field No.1"] = inArray[20];
               Elements["Lab Field No.2"] = inArray[21];
               Elements["Date/Time results reported or last modified"] = inArray[22];
               Elements["Instrument Charge to Computer System"] = inArray[23];
               Elements["Instrument Section ID"] = inArray[24];
               Elements["Report Types"] = inArray[25];
               Elements["Reserved Field"] = inArray[26];
               Elements["Location or ward of Specimen Collection"] = inArray[27];
               Elements["Nosocomial Infection Flag"] = inArray[28];
               Elements["Specimen Service"] = inArray[29];
               Elements["Specimen Institution"] = inArray[30];
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