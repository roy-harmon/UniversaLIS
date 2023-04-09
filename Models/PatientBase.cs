using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     public class PatientBase
     {
          private OrderedDictionary elements = new OrderedDictionary();
          private List<OrderBase> orders = new List<OrderBase>();
          private int patientID;
          [JsonPropertyOrder(18)]
          public string? ActiveMeds { get => (string?)Elements["ActiveMeds"]; set => Elements["ActiveMeds"] = value; }
          [JsonPropertyOrder(9)]
          public string? Address { get => (string?)Elements["Address"]; set => Elements["Address"] = value; }
          [JsonPropertyOrder(22)]
          public string? AdmDates { get => (string?)Elements["AdmDates"]; set => Elements["AdmDates"] = value; }
          [JsonPropertyOrder(23)]
          public string? AdmStatus { get => (string?)Elements["AdmStatus"]; set => Elements["AdmStatus"] = value; }
          [JsonPropertyOrder(26)]
          public string? AltCode { get => (string?)Elements["AltCode"]; set => Elements["AltCode"] = value; }
          [JsonPropertyOrder(25)]
          public string? AltCodeNature { get => (string?)Elements["AltCodeNature"]; set => Elements["AltCodeNature"] = value; }
          [JsonPropertyOrder(12)]
          public string? AttendingPhysicianID { get => (string?)Elements["AttendingPhysicianID"]; set => Elements["AttendingPhysicianID"] = value; }
          [JsonPropertyOrder(17)]
          public string? Diagnosis { get => (string?)Elements["Diagnosis"]; set => Elements["Diagnosis"] = value; }
          [JsonPropertyOrder(19)]
          public string? Diet { get => (string?)Elements["Diet"]; set => Elements["Diet"] = value; }
          [JsonPropertyOrder(6)]
          public string? DOB { get => (string?)Elements["DOB"]; set => Elements["DOB"] = value; }
          [JsonPropertyOrder(33)]
          public string? DosageCategory { get => (string?)Elements["DosageCategory"]; set => Elements["DosageCategory"] = value; }
          [NotMapped, JsonIgnore]
          public OrderedDictionary Elements { get => elements; set => elements = value; }
          [JsonPropertyOrder(15)]
          public string? Height { get => (string?)Elements["Height"]; set => Elements["Height"] = value; }
          [JsonPropertyOrder(32)]
          public string? HospInstitution { get => (string?)Elements["HospInstitution"]; set => Elements["HospInstitution"] = value; }
          [JsonPropertyOrder(31)]
          public string? HospService { get => (string?)Elements["HospService"]; set => Elements["HospService"] = value; }
          [JsonPropertyOrder(29)]
          public string? IsolationStatus { get => (string?)Elements["IsolationStatus"]; set => Elements["IsolationStatus"] = value; }
          [JsonPropertyOrder(2)]
          public string? LabPatientID { get => (string?)Elements["LabPatientID"]; set => Elements["LabPatientID"] = value; }
          [JsonPropertyOrder(30)]
          public string? Language { get => (string?)Elements["Language"]; set => Elements["Language"] = value; }
          [JsonPropertyOrder(24)]
          public string? Location { get => (string?)Elements["Location"]; set => Elements["Location"] = value; }
          [JsonPropertyOrder(28)]
          public string? MaritalStatus { get => (string?)Elements["MaritalStatus"]; set => Elements["MaritalStatus"] = value; }
          [JsonPropertyOrder(5)]
          public string? MMName { get => (string?)Elements["MMName"]; set => Elements["MMName"] = value; }
          [JsonPropertyOrder(100)]
          public virtual List<OrderBase> Orders { get => orders; set => orders = value; }

          [JsonPropertyOrder(0)]
          [Key]
          public virtual int PatientID { get => patientID; set => patientID = value; }
          [JsonPropertyOrder(3)]
          public string? PatientID3 { get => (string?)Elements["PatientID3"]; set => Elements["PatientID3"] = value; }
          [JsonPropertyOrder(4)]
          public string? PatientName { get => (string?)Elements["PatientName"]; set => Elements["PatientName"] = value; }
          [JsonPropertyOrder(20)]
          public string? PF1 { get => (string?)Elements["PF1"]; set => Elements["PF1"] = value; }
          [JsonPropertyOrder(21)]
          public string? PF2 { get => (string?)Elements["PF2"]; set => Elements["PF2"] = value; }
          [JsonPropertyOrder(1)]

          public string? PracticePatientID { get => (string?)Elements["PracticePatientID"]; set => Elements["PracticePatientID"] = value; }
          [JsonPropertyOrder(8)]
          public string? Race { get => (string?)Elements["Race"]; set => Elements["Race"] = value; }
          [JsonPropertyOrder(27)]
          public string? Religion { get => (string?)Elements["Religion"]; set => Elements["Religion"] = value; }
          [JsonPropertyOrder(10)]
          public string? Reserved { get => (string?)Elements["Reserved"]; set => Elements["Reserved"] = value; }
          [JsonPropertyOrder(7)]
          public string? Sex { get => (string?)Elements["Sex"]; set => Elements["Sex"] = value; }
          [JsonPropertyOrder(13)]
          public string? Special1 { get => (string?)Elements["Special1"]; set => Elements["Special1"] = value; }
          [JsonPropertyOrder(14)]
          public string? Special2 { get => (string?)Elements["Special2"]; set => Elements["Special2"] = value; }
          [JsonPropertyOrder(11)]
          public string? TelNo { get => (string?)Elements["TelNo"]; set => Elements["TelNo"] = value; }
          [JsonPropertyOrder(16)]
          public string? Weight { get => (string?)Elements["Weight"]; set => Elements["Weight"] = value; }

          private string GetPatientString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence#", "PracticePatientID", "LabPatientID", "PatientID3", "PatientName", "MMName", "DOB", "Sex", "Race", "Address", "Reserved", "TelNo", "AttendingPhysicianID", "Special1", "Special2", "Height", "Weight", "Diagnosis", "ActiveMeds", "Diet", "PF1", "PF2", "AdmDates", "AdmStatus", "Location", "AltCodeNature", "AltCode", "Religion", "MaritalStatus", "IsolationStatus", "Language", "HospService", "HospInstitution", "DosageCategory" };
               foreach (var item in elementArray)
               {
                    if (!Elements.Contains(item))
                    {
                         Elements.Add(item, "");
                    }
               }

               string output = Constants.STX + $"{Elements["FrameNumber"]}".Trim('P') + "P|";
               // Concatenate the Dictionary values and return the string.
               output += Elements["Sequence#"] + "|";
               output += Elements["PracticePatientID"] + "|";
               output += Elements["LabPatientID"] + "|";
               output += Elements["PatientID3"] + "|";
               output += Elements["PatientName"] + "|";
               output += Elements["MMName"] + "|";
               output += Elements["DOB"] + "|";
               output += Elements["Sex"] + "|";
               output += Elements["Race"] + "|";
               output += Elements["Address"] + "|";
               output += Elements["Reserved"] + "|";
               output += Elements["TelNo"] + "|";
               output += Elements["AttendingPhysicianID"] + "|";
               output += Elements["Special1"] + "|";
               output += Elements["Special2"] + "|";
               output += Elements["Height"] + "|";
               output += Elements["Weight"] + "|";
               output += Elements["Diagnosis"] + "|";
               output += Elements["ActiveMeds"] + "|";
               output += Elements["Diet"] + "|";
               output += Elements["PF1"] + "|";
               output += Elements["PF2"] + "|";
               output += Elements["AdmDates"] + "|";
               output += Elements["AdmStatus"] + "|";
               output += Elements["Location"] + "|";
               output += Elements["AltCodeNature"] + "|";
               output += Elements["AltCode"] + "|";
               output += Elements["Religion"] + "|";
               output += Elements["MaritalStatus"] + "|";
               output += Elements["IsolationStatus"] + "|";
               output += Elements["Language"] + "|";
               output += Elements["HospService"] + "|";
               output += Elements["HospInstitution"] + "|";
               output += Elements["DosageCategory"] + Constants.CR + Constants.ETX;
               return output;
          }

          public string GetPatientMessage()
          {
               return GetPatientString();
          }

          private protected void SetPatientString(string input)
          {
               string[] inArray = input.Split('|');
               if (inArray.Length < 35)
               {
                    // Invalid number of elements.
                    throw new Exception($"Invalid number of elements in patient record string. Expected: 35 \tFound: {inArray.Length} \tString: \n{input}");
               }
               Elements["FrameNumber"] = inArray[0];
               Elements["Sequence#"] = inArray[1];
               Elements["PracticePatientID"] = inArray[2];
               Elements["LabPatientID"] = inArray[3];
               Elements["PatientID3"] = inArray[4];
               Elements["PatientName"] = inArray[5];
               Elements["MMName"] = inArray[6];
               Elements["DOB"] = inArray[7];
               Elements["Sex"] = inArray[8];
               Elements["Race"] = inArray[9];
               Elements["Address"] = inArray[10];
               Elements["Reserved"] = inArray[11];
               Elements["TelNo"] = inArray[12];
               Elements["AttendingPhysicianID"] = inArray[13];
               Elements["Special1"] = inArray[14];
               Elements["Special2"] = inArray[15];
               Elements["Height"] = inArray[16];
               Elements["Weight"] = inArray[17];
               Elements["Diagnosis"] = inArray[18];
               Elements["ActiveMeds"] = inArray[19];
               Elements["Diet"] = inArray[20];
               Elements["PF1"] = inArray[21];
               Elements["PF2"] = inArray[22];
               Elements["AdmDates"] = inArray[23];
               Elements["AdmStatus"] = inArray[24];
               Elements["Location"] = inArray[25];
               Elements["AltCodeNature"] = inArray[26];
               Elements["AltCode"] = inArray[27];
               Elements["Religion"] = inArray[28];
               Elements["MaritalStatus"] = inArray[29];
               Elements["IsolationStatus"] = inArray[30];
               Elements["Language"] = inArray[31];
               Elements["HospService"] = inArray[32];
               Elements["HospInstitution"] = inArray[33];
               Elements["DosageCategory"] = inArray[34];
          }

          public void SetPatientMessage(string value)
          {
               SetPatientString(value);
          }
     }
}