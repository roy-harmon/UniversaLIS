using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMMULIS
{
     public class Patient
     {
          public Dictionary<string, string> Elements = new Dictionary<string, string>();

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

          private string GetPatientString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Sequence #", "Practice-Assigned Patient ID", "Laboratory Assigned Patient ID", "Patient ID", "Patient Name", "Mother's Maiden Name", "BirthDate", "Patient Sex", "Patient Race", "Patient Address", "Reserved", "Patient Phone #", "Attending Physician ID", "Special Field 1", "Special Field 2", "Patient Height", "Patient Weight", "Patients Known or Suspected Diagnosis", "Patient active medications", "Patients Diet", "Practice Field #1", "Practice Field #2", "Admission and Discharge Dates", "Admission Status", "Location", "Nature of Alternative Diagnostic Code and Classification", "Alternative Diagnostic Code and Classification", "Patient Religion", "Marital Status", "Isolation Status", "Language", "Hospital Service", "Hospital Institution", "Dosage Category" };
               foreach (var item in elementArray)
               {
                    if (!Elements.ContainsKey(item))
                    {
                         Elements.Add(item, "");
                    }
               }

               string output = Constants.STX + Elements["FrameNumber"].Trim('P') + "P|";
               // Concatenate the Dictionary values and return the string.
               output += Elements["Sequence #"] + "|";
               output += Elements["Practice-Assigned Patient ID"] + "|";
               output += Elements["Laboratory Assigned Patient ID"] + "|";
               output += Elements["Patient ID"] + "|";
               output += Elements["Patient Name"] + "|";
               output += Elements["Mother's Maiden Name"] + "|";
               output += Elements["BirthDate"] + "|";
               output += Elements["Patient Sex"] + "|";
               output += Elements["Patient Race"] + "|";
               output += Elements["Patient Address"] + "|";
               output += Elements["Reserved"] + "|";
               output += Elements["Patient Phone #"] + "|";
               output += Elements["Attending Physician ID"] + "|";
               output += Elements["Special Field 1"] + "|";
               output += Elements["Special Field 2"] + "|";
               output += Elements["Patient Height"] + "|";
               output += Elements["Patient Weight"] + "|";
               output += Elements["Patients Known or Suspected Diagnosis"] + "|";
               output += Elements["Patient active medications"] + "|";
               output += Elements["Patients Diet"] + "|";
               output += Elements["Practice Field #1"] + "|";
               output += Elements["Practice Field #2"] + "|";
               output += Elements["Admission and Discharge Dates"] + "|";
               output += Elements["Admission Status"] + "|";
               output += Elements["Location"] + "|";
               output += Elements["Nature of Alternative Diagnostic Code and Classification"] + "|";
               output += Elements["Alternative Diagnostic Code and Classification"] + "|";
               output += Elements["Patient Religion"] + "|";
               output += Elements["Marital Status"] + "|";
               output += Elements["Isolation Status"] + "|";
               output += Elements["Language"] + "|";
               output += Elements["Hospital Service"] + "|";
               output += Elements["Hospital Institution"] + "|";
               output += Elements["Dosage Category"] + Constants.CR + Constants.ETX;
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
               Elements["Sequence #"] = inArray[1];
               Elements["Practice-Assigned Patient ID"] = inArray[2];
               Elements["Laboratory Assigned Patient ID"] = inArray[3];
               Elements["Patient ID"] = inArray[4];
               Elements["Patient Name"] = inArray[5];
               Elements["Mother's Maiden Name"] = inArray[6];
               Elements["BirthDate"] = inArray[7];
               Elements["Patient Sex"] = inArray[8];
               Elements["Patient Race"] = inArray[9];
               Elements["Patient Address"] = inArray[10];
               Elements["Reserved"] = inArray[11];
               Elements["Patient Phone #"] = inArray[12];
               Elements["Attending Physician ID"] = inArray[13];
               Elements["Special Field 1"] = inArray[14];
               Elements["Special Field 2"] = inArray[15];
               Elements["Patient Height"] = inArray[16];
               Elements["Patient Weight"] = inArray[17];
               Elements["Patients Known or Suspected Diagnosis"] = inArray[18];
               Elements["Patient active medications"] = inArray[19];
               Elements["Patients Diet"] = inArray[20];
               Elements["Practice Field #1"] = inArray[21];
               Elements["Practice Field #2"] = inArray[22];
               Elements["Admission and Discharge Dates"] = inArray[23];
               Elements["Admission Status"] = inArray[24];
               Elements["Location"] = inArray[25];
               Elements["Nature of Alternative Diagnostic Code and Classification"] = inArray[26];
               Elements["Alternative Diagnostic Code and Classification"] = inArray[27];
               Elements["Patient Religion"] = inArray[28];
               Elements["Marital Status"] = inArray[29];
               Elements["Isolation Status"] = inArray[30];
               Elements["Language"] = inArray[31];
               Elements["Hospital Service"] = inArray[32];
               Elements["Hospital Institution"] = inArray[33];
               Elements["Dosage Category"] = inArray[34];
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