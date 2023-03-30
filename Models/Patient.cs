using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Core.Events;

[assembly: InternalsVisibleTo("UniLisApi")]

namespace UniversaLIS.Models
{
     public class Patient
     {
          private OrderedDictionary elements = new OrderedDictionary();
          private protected int patientID;

          private List<Order> orders = new List<Order>();
          private List<Comment> comments = new List<Comment>();

          public string GetPatientMessage()
          {
               return GetPatientString();
          }

          public void SetPatientMessage(string value)
          {
               SetPatientString(value);
          }

          [Key]
          public int PatientID { get => patientID; set => patientID = value; }
          [NotMapped]
          [JsonIgnore]
          public OrderedDictionary Elements { get => elements; set => elements = value; }

          public string? PracticePatientID { get => $"{Elements["PracticePatientID"]}"; set => Elements["PracticePatientID"] = value ?? ""; }
          public string? LabPatientID { get => $"{Elements["LabPatientID"]}"; set => Elements["LabPatientID"] = value ?? ""; }
          public string? PatientID3 { get => $"{Elements["PatientID3"]}"; set => Elements["PatientID3"] = value ?? ""; }
          public string? PatientName { get => $"{Elements["PatientName"]}"; set => Elements["PatientName"] = value ?? ""; }
          public string? MMName { get => $"{Elements["MMName"]}"; set => Elements["MMName"] = value ?? ""; }
          public string? DOB { get => $"{Elements["DOB"]}"; set => Elements["DOB"] = value ?? ""; }
          public string? Sex { get => $"{Elements["Sex"]}"; set => Elements["Sex"] = value ?? ""; }
          public string? Race { get => $"{Elements["Race"]}"; set => Elements["Race"] = value ?? ""; }
          public string? Address { get => $"{Elements["Address"]}"; set => Elements["Address"] = value ?? ""; }
          public string? Reserved { get => $"{Elements["Reserved"]}"; set => Elements["Reserved"] = value ?? ""; }
          public string? TelNo { get => $"{Elements["TelNo"]}"; set => Elements["TelNo"] = value ?? ""; }
          public string? AttendingPhysicianID { get => $"{Elements["AttendingPhysicianID"]}"; set => Elements["AttendingPhysicianID"] = value ?? ""; }
          public string? Special1 { get => $"{Elements["Special1"]}"; set => Elements["Special1"] = value ?? ""; }
          public string? Special2 { get => $"{Elements["Special2"]}"; set => Elements["Special2"] = value ?? ""; }
          public string? Height { get => $"{Elements["Height"]}"; set => Elements["Height"] = value ?? ""; }
          public string? Weight { get => $"{Elements["Weight"]}"; set => Elements["Weight"] = value ?? ""; }
          public string? Diagnosis { get => $"{Elements["Diagnosis"]}"; set => Elements["Diagnosis"] = value ?? ""; }
          public string? ActiveMeds { get => $"{Elements["ActiveMeds"]}"; set => Elements["ActiveMeds"] = value ?? ""; }
          public string? Diet { get => $"{Elements["Diet"]}"; set => Elements["Diet"] = value ?? ""; }
          public string? PF1 { get => $"{Elements["PF1"]}"; set => Elements["PF1"] = value ?? ""; }
          public string? PF2 { get => $"{Elements["PF2"]}"; set => Elements["PF2"] = value ?? ""; }
          public string? AdmDates { get => $"{Elements["AdmDates"]}"; set => Elements["AdmDates"] = value ?? ""; }
          public string? AdmStatus { get => $"{Elements["AdmStatus"]}"; set => Elements["AdmStatus"] = value ?? ""; }
          public string? Location { get => $"{Elements["Location"]}"; set => Elements["Location"] = value ?? ""; }
          public string? AltCodeNature { get => $"{Elements["AltCodeNature"]}"; set => Elements["AltCodeNature"] = value ?? ""; }
          public string? AltCode { get => $"{Elements["AltCode"]}"; set => Elements["AltCode"] = value ?? ""; }
          public string? Religion { get => $"{Elements["Religion"]}"; set => Elements["Religion"] = value ?? ""; }
          public string? MaritalStatus { get => $"{Elements["MaritalStatus"]}"; set => Elements["MaritalStatus"] = value ?? ""; }
          public string? IsolationStatus { get => $"{Elements["IsolationStatus"]}"; set => Elements["IsolationStatus"] = value ?? ""; }
          public string? Language { get => $"{Elements["Language"]}"; set => Elements["Language"] = value ?? ""; }
          public string? HospService { get => $"{Elements["HospService"]}"; set => Elements["HospService"] = value ?? ""; }
          public string? HospInstitution { get => $"{Elements["HospInstitution"]}"; set => Elements["HospInstitution"] = value ?? ""; }
          public string? DosageCategory { get => $"{Elements["DosageCategory"]}"; set => Elements["DosageCategory"] = value ?? ""; }
          public List<Order> Orders { get => orders; set => orders = value; }
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

          private void SetPatientString(string input)
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

          public Patient(string patientMessage)
          {
               SetPatientString(patientMessage);
          }

          public Patient()
          {
               SetPatientString("|1|||||||||||||||||||||||||||||||||");
          }
          
          public override string ToString()
          {
               return GetJsonString();
          }
          
          public string GetJsonString()
          {
               string[] details = { "PracticePatientID", "LabPatientID", "PatientID3", "PatientName", "MMName", "DOB", "Sex", "Race", "Address", "Reserved",
                    "TelNo", "AttendingPhysicianID", "Special1", "Special2", "Height", "Weight", "Diagnosis", "ActiveMeds", "Diet", "PF1", "PF2", "AdmDates",
                    "AdmStatus", "Location", "AltCodeNature", "AltCode", "Religion", "MaritalStatus", "IsolationStatus", "Language", "HospService", "HospInstitution", "DosageCategory" };
               OrderedDictionary fieldList = new OrderedDictionary();
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
               fieldList.Add("Orders", orders);
               return JsonSerializer.Serialize(fieldList);
          }
     }

}
