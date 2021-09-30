using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniversaLIS
{
     public class Result
     {
          public Dictionary<string, string> Elements = new Dictionary<string, string>();

          public string ResultMessage
          {
               get => GetResultString();
               set => SetResultString(value);
          }

          private string GetResultString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence #", "Universal Test ID", "Data (result)", "Units", "Reference Ranges", "Result abnormal flags", "Nature of Abnormality Testing", "Result Status", "Date of change in instruments normal values or units", "Operator ID", "Date/Time Test Started", "Date/Time Test Completed", "Instrument ID" };
               foreach (var item in elementArray)
               {
                    if (!Elements.ContainsKey(item))
                    {
                         Elements.Add(item, "");
                    }
               }
               string output = Constants.STX + Elements["FrameNumber"].Trim('R') + "R|";
               // Concatenate the Dictionary values and return the string.
               output += Elements["Sequence #"] + "|";
               output += Elements["Universal Test ID"] + "|";
               output += Elements["Data (result)"] + "|";
               output += Elements["Units"] + "|";
               output += Elements["Reference Ranges"] + "|";
               output += Elements["Result abnormal flags"] + "|";
               output += Elements["Nature of Abnormality Testing"] + "|";
               output += Elements["Result Status"] + "|";
               output += Elements["Date of change in instruments normal values or units"] + "|";
               output += Elements["Operator ID"] + "|";
               output += Elements["Date/Time Test Started"] + "|";
               output += Elements["Date/Time Test Completed"] + "|";
               output += Elements["Instrument ID"] + Constants.CR + Constants.ETX;
               return output;
          }

          private void SetResultString(string input)
          {
               string[] inArray = input.Split('|');
               if (inArray.Length < 14)
               {
                    // Invalid number of elements.
                    throw new Exception($"Invalid number of elements in result record string. Expected: 14 \tFound: {inArray.Length} \tString: \n{input}");
               }
               Elements["FrameNumber"] = inArray[0];
               Elements["Sequence #"] = inArray[1];
               Elements["Universal Test ID"] = inArray[2];
               Elements["Data (result)"] = inArray[3];
               Elements["Units"] = inArray[4];
               Elements["Reference Ranges"] = inArray[5];
               Elements["Result abnormal flags"] = inArray[6];
               Elements["Nature of Abnormality Testing"] = inArray[7];
               Elements["Result Status"] = inArray[8];
               Elements["Date of change in instruments normal values or units"] = inArray[9];
               Elements["Operator ID"] = inArray[10];
               Elements["Date/Time Test Started"] = inArray[11];
               Elements["Date/Time Test Completed"] = inArray[12];
               Elements["Instrument ID"] = inArray[13].Substring(0, inArray[13].IndexOf(Constants.CR));
          }

          public Result(string resultMessage)
          {
               SetResultString(resultMessage);
          }
     }
}