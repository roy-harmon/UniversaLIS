using Swashbuckle.AspNetCore.Annotations;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     [Table("ResultRecord"), SwaggerSchema("Each result record contains the results of a single analytic determination.")]
     public class Result
     {
          private OrderedDictionary elements = new();
          private List<Comment> comments = new();
          private int resultID;
          private int orderID;

          [Key]
          public int ResultID { get => resultID; set => resultID = value; }
          [JsonIgnore]
          [ForeignKey(nameof(OrderID))]
          [InverseProperty("Results")]
          public int OrderID { get => orderID; set => orderID = value; }
          public string? UniversalTestID { get => (string?)Elements["UniversalTestID"]; set => Elements["UniversalTestID"] = value; }
          public string? ResultValue { get => (string?)Elements["ResultValue"]; set => Elements["ResultValue"] = value; }
          public string? Unit { get => (string?)Elements["Unit"]; set => Elements["Unit"] = value; }
          public string? RefRange { get => (string?)Elements["RefRange"]; set => Elements["RefRange"] = value; }
          public string? Abnormal { get => (string?)Elements["Abnormal"]; set => Elements["Abnormal"] = value; }
          public string? AbNature { get => (string?)Elements["AbNature"]; set => Elements["AbNature"] = value; }
          public string? ResStatus { get => (string?)Elements["ResStatus"]; set => Elements["ResStatus"] = value; }
          public string? NormsChanged { get => (string?)Elements["NormsChanged"]; set => Elements["NormsChanged"] = value; }
          public string? OperatorID { get => (string?)Elements["OperatorID"]; set => Elements["OperatorID"] = value; }
          public string? TestStart { get => (string?)Elements["TestStart"]; set => Elements["TestStart"] = value; }
          public string? TestEnd { get => (string?)Elements["TestEnd"]; set => Elements["TestEnd"] = value; }
          public string? InstrumentID { get => (string?)Elements["InstrumentID"]; set => Elements["InstrumentID"] = value; }

          public string GetResultMessage()
          {
               return GetResultString();
          }

          public void SetResultMessage(string value)
          {
               SetResultString(value);
          }

          [NotMapped]
          [JsonIgnore]
          public OrderedDictionary Elements { get => elements; set => elements = value; }
          internal List<Comment> Comments { get => comments; set => comments = value; }

          private string GetResultString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence#", "UniversalTestID", "ResultValue", "Unit", "RefRange", "Abnormal", "AbNature", "ResStatus", "NormsChanged", "OperatorID", "TestStart", "TestEnd", "InstrumentID" };
               foreach (var item in elementArray)
               {
                    if (!Elements.Contains(item))
                    {
                         Elements.Add(item, "");
                    }
               }
               string output = Constants.STX + $"{Elements["FrameNumber"]}".Trim('R') + "R|";
               // Concatenate the Dictionary values and return the string.
               output += Elements["Sequence#"] + "|";
               output += Elements["UniversalTestID"] + "|";
               output += Elements["ResultValue"] + "|";
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
               Elements["ResultValue"] = inArray[3];
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

          public Result() { }

          public string GetJsonString()
          {
               OrderedDictionary fieldList = new();
               IDictionaryEnumerator enumerator = elements.GetEnumerator();
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
               return JsonSerializer.Serialize(fieldList);
          }
     }
}