using System;
using System.Collections.Generic;

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
               string[] elementArray = { "FrameNumber", "Sequence#", "UniversalTestID", "Result", "Unit", "RefRange", "Abnormal", "AbNature", "ResStatus", "NormsChanged", "OperatorID", "TestStart", "TestEnd", "InstrumentID" };
               foreach (var item in elementArray)
               {
                    if (!Elements.ContainsKey(item))
                    {
                         Elements.Add(item, "");
                    }
               }
               string output = Constants.STX + Elements["FrameNumber"].Trim('R') + "R|";
               // Concatenate the Dictionary values and return the string.
               output += Elements["Sequence#"] + "|";
               output += Elements["UniversalTestID"] + "|";
               output += Elements["Result"] + "|";
               output += Elements["Unit"] + "|";
               output += Elements["RefRange"] + "|";
               output += Elements["Abnormal"] + "|";
               output += Elements["AbNature"] + "|";
               output += Elements["ResStatus"] + "|";
               output += Elements["NormsChanged"] + "|";
               output += Elements["OperatorID"] + "|";
               output += Elements["TestStart"] + "|";
               output += Elements["TestEnd"] + "|";
               output += Elements["InstrumentID"] + Constants.CR + Constants.ETX;
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
               Elements["Sequence#"] = inArray[1];
               Elements["UniversalTestID"] = inArray[2];
               Elements["Result"] = inArray[3];
               Elements["Unit"] = inArray[4];
               Elements["RefRange"] = inArray[5];
               Elements["Abnormal"] = inArray[6];
               Elements["AbNature"] = inArray[7];
               Elements["ResStatus"] = inArray[8];
               Elements["NormsChanged"] = inArray[9];
               Elements["OperatorID"] = inArray[10];
               Elements["TestStart"] = inArray[11];
               Elements["TestEnd"] = inArray[12];
               Elements["InstrumentID"] = inArray[13].Substring(0, inArray[13].IndexOf(Constants.CR));
          }

          public Result(string resultMessage)
          {
               SetResultString(resultMessage);
          }
     }
}