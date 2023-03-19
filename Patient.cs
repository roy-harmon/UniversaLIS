using System;
using System.Collections.Generic;

namespace UniversaLIS
{
     public class Patient
     {
          public Dictionary<string, string> Elements = new Dictionary<string, string>();
          public int PatientID;

          public List<Order> Orders = new List<Order>();

          public string PatientMessage
          {
               get
               {
                    return GetPatientString();
               }
               set
               {
                    SetPatientString(value);
               }
          }
          // It would probably be a good idea to make each of the various array elements into a property of the Patient class.
          // Maybe consider that for a future update.
          private string GetPatientString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence#", "PracticePatientID", "LabPatientID", "PatientID3", "PatientName", "MMName", "DOB", "Sex", "Race", "Address", "Reserved", "TelNo", "AttendingPhysicianID", "Special1", "Special2", "Height", "Weight", "Diagnosis", "ActiveMeds", "Diet", "PF1", "PF2", "AdmDates", "AdmStatus", "Location", "AltCodeNature", "AltCode", "Religion", "MaritalStatus", "IsolationStatus", "Language", "HospService", "HospInstitution", "DosageCategory" };
               foreach (var item in elementArray)
               {
                    if (!Elements.ContainsKey(item))
                    {
                         Elements.Add(item, "");
                    }
               }

               string output = Constants.STX + Elements["FrameNumber"].Trim('P') + "P|";
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
     }
}