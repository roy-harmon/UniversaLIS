using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UniversaLIS.Models
{
     [SwaggerSchema("This record type contains information about an individual patient.")]
     public class PatientBase
     {
          private OrderedDictionary elements = new();
          private List<OrderBase> orders = new();
          private int patientID;
          [NotMapped, JsonIgnore]
          public OrderedDictionary Elements { get => elements; set => elements = value; }

          [JsonPropertyOrder(0), SwaggerSchema("The unique identifier assigned to the order record in the UniversaLIS internal database.", ReadOnly = true)]
          [Key]
          public virtual int PatientID { get => patientID; set => patientID = value; }
          [JsonPropertyOrder(1), SwaggerSchema("The unique ID assigned and used by the practice to identify the patient and his/her results upon return of the results of testing.")]

          public string? PracticePatientID { get => (string?)Elements["PracticePatientID"]; set => Elements["PracticePatientID"] = value; }

          [JsonPropertyOrder(2), SwaggerSchema("The unique processing number assigned to the patient by the laboratory.")]
          public string? LabPatientID { get => (string?)Elements["LabPatientID"]; set => Elements["LabPatientID"] = value; }

          [JsonPropertyOrder(3), SwaggerSchema("Optionally used for additional, universal, or manufacturer-defined identifiers.")]
          public string? PatientID3 { get => (string?)Elements["PatientID3"]; set => Elements["PatientID3"] = value; }

          [JsonPropertyOrder(4), SwaggerSchema("The patient’s name shall be presented in the following format: last name, first name, middle name or initial, " +
               "suffix, and title, and each of these components shall be separated by a component delimiter (^).")]
          public string? PatientName { get => (string?)Elements["PatientName"]; set => Elements["PatientName"] = value; }

          [JsonPropertyOrder(5), SwaggerSchema("The optional mother’s maiden name may be required to distinguish between patients with the same birthdate and last name when registry files are very large.")]
          public string? MMName { get => (string?)Elements["MMName"]; set => Elements["MMName"] = value; }

          [JsonPropertyOrder(6), SwaggerSchema("The patient's birthdate, presented in YYYYMMDD format.", Format = "yyyyMMdd")]
          public string? DOB { get => (string?)Elements["DOB"]; set => Elements["DOB"] = value; }

          [JsonPropertyOrder(7), SwaggerSchema("This field shall be represented by M, F, or U.")]
          public string? Sex { get => (string?)Elements["Sex"]; set => Elements["Sex"] = value; }

          [JsonPropertyOrder(8), SwaggerSchema("Abbreviation or full text names of ethnic groups may be entered. Note that multiple answers are permissible, separated by a component delimiter (^).")]
          public string? Race { get => (string?)Elements["Race"]; set => Elements["Race"] = value; }

          [JsonPropertyOrder(9), SwaggerSchema("The street address of the patient’s mailing address.")]
          public string? Address { get => (string?)Elements["Address"]; set => Elements["Address"] = value; }

          [JsonPropertyOrder(10), SwaggerSchema("This field is reserved for future expansion.")]
          public string? Reserved { get => (string?)Elements["Reserved"]; set => Elements["Reserved"] = value; }

          [JsonPropertyOrder(11), SwaggerSchema("The patient’s telephone number.")]
          public string? TelNo { get => (string?)Elements["TelNo"]; set => Elements["TelNo"] = value; }

          [JsonPropertyOrder(12), SwaggerSchema("The physician(s) caring for the patient as either names or codes, as agreed upon between the sender and the receiver. Identifiers or names, or both, should be separated by component delimiters (^) and formatted like the PatientName field. Multiple physician names (for example, ordering physician, attending physician, referring physician) shall be separated by repeat delimiters (\\).")]
          public string? AttendingPhysicianID { get => (string?)Elements["AttendingPhysicianID"]; set => Elements["AttendingPhysicianID"] = value; }

          [JsonPropertyOrder(13), SwaggerSchema("Optional text field for vendor use (each laboratory can use this differently).")]
          public string? Special1 { get => (string?)Elements["Special1"]; set => Elements["Special1"] = value; }

          [JsonPropertyOrder(14), SwaggerSchema("Optional text field for vendor use (each laboratory can use this differently).")]
          public string? Special2 { get => (string?)Elements["Special2"]; set => Elements["Special2"] = value; }

          [JsonPropertyOrder(15), SwaggerSchema("Optional numeric field containing the patient’s height. The default units are centimeters. If measured in terms of another unit, the units should also be transmitted separated by a component delimiter (^).")]
          public string? Height { get => (string?)Elements["Height"]; set => Elements["Height"] = value; }

          [JsonPropertyOrder(16), SwaggerSchema("Optional numeric field containing the patient’s weight. The default units are kilograms. If measured in terms of another unit, for example, pounds, the unit name shall also be transmitted separated by a component delimiter (^).")]
          public string? Weight { get => (string?)Elements["Weight"]; set => Elements["Weight"] = value; }

          [JsonPropertyOrder(17), SwaggerSchema("This value should be entered either as an ICD-9 code or as free text. If multiple diagnoses are recorded, they shall be separated by repeat delimiters (\\).")]
          public string? Diagnosis { get => (string?)Elements["Diagnosis"]; set => Elements["Diagnosis"] = value; }

          [JsonPropertyOrder(18), SwaggerSchema("This field is used for patient active medications or those suspected, in overdose situations. The generic name shall be used. This field is of use in interpretation of clinical results.")]
          public string? ActiveMeds { get => (string?)Elements["ActiveMeds"]; set => Elements["ActiveMeds"] = value; }

          [JsonPropertyOrder(19), SwaggerSchema("This optional field in free text should be used to indicate such conditions that affect results of testing, such as 16-hour fast (for triglycerides) and no red meat (for hemoccult testing).")]
          public string? Diet { get => (string?)Elements["Diet"]; set => Elements["Diet"] = value; }

          [JsonPropertyOrder(20), SwaggerSchema("This is a text field for use by the practice; the optional transmitted text will be returned with the results.")]
          public string? PF1 { get => (string?)Elements["PF1"]; set => Elements["PF1"] = value; }

          [JsonPropertyOrder(21), SwaggerSchema("This is a text field for use by the practice; the optional transmitted text will be returned with the results.")]
          public string? PF2 { get => (string?)Elements["PF2"]; set => Elements["PF2"] = value; }

          [JsonPropertyOrder(22), SwaggerSchema("Admission and discharge dates, in YYYYMMDD format. The discharge date, when included, follows the admission date and is separated from it by a repeat delimiter (\\).")]
          public string? AdmDates { get => (string?)Elements["AdmDates"]; set => Elements["AdmDates"] = value; }

          [JsonPropertyOrder(23), SwaggerSchema("Admission status, represented by the following minimal list or by extensions agreed upon between the sender and receiver: OP (outpatient), PA (preadmit), IP (inpatient), ER (emergency room).")]
          public string? AdmStatus { get => (string?)Elements["AdmStatus"]; set => Elements["AdmStatus"] = value; }

          [JsonPropertyOrder(24), SwaggerSchema("The general clinic location or nursing unit, or ward or bed (or both) of the patient.")]
          public string? Location { get => (string?)Elements["Location"]; set => Elements["Location"] = value; }

          [JsonPropertyOrder(25), SwaggerSchema("Nature of Alternative Diagnostic Code and Classifiers. It identifies the class of code or classifiers that are transmitted (e.g., DRGs, or in the future, AVGs [ambulatory visitation groups]).")]
          public string? AltCodeNature { get => (string?)Elements["AltCodeNature"]; set => Elements["AltCodeNature"] = value; }

          [JsonPropertyOrder(26), SwaggerSchema("Alternative diagnostic codes and classifications (e.g., DRG codes) can be included in this field. The nature of the diagnostic code " +
               "is identified in the AltCodeNature field. If multiple codes are included, they should be separated by repeat delimiters (\\). Individual codes can be followed by optional test " +
               "descriptors (when the latter are present) and must be separated by component delimiters (^).")]
          public string? AltCode { get => (string?)Elements["AltCode"]; set => Elements["AltCode"] = value; }

          [JsonPropertyOrder(27), SwaggerSchema("When needed, this value shall include the patient’s religion. Codes or names may be sent as agreed upon between the sender and the " +
               "receiver. Full names of religions may also be sent as required.")]
          public string? Religion { get => (string?)Elements["Religion"]; set => Elements["Religion"] = value; }

          [JsonPropertyOrder(28), SwaggerSchema("When required, this value shall indicate the marital status of the patient as follows:<br>M = married<br>S = single<br>D = divorced<br>W = widowed<br>A = separated")]
          public string? MaritalStatus { get => (string?)Elements["MaritalStatus"]; set => Elements["MaritalStatus"] = value; }

          [JsonPropertyOrder(29), SwaggerSchema("Isolation codes indicate precautions that must be applied to protect the patient or staff against infection. Multiple precautions can be listed when separated by repeat delimiters (\\). Full text precautions may also be sent.")]
          public string? IsolationStatus { get => (string?)Elements["IsolationStatus"]; set => Elements["IsolationStatus"] = value; }

          [JsonPropertyOrder(30), SwaggerSchema("The patient’s primary language. This may be needed when the patient is not fluent in the local language.")]
          public string? Language { get => (string?)Elements["Language"]; set => Elements["Language"] = value; }

          [JsonPropertyOrder(31), SwaggerSchema("The hospital service currently assigned to the patient. Both code and text may be sent when separated by a component delimiter (^).")]
          public string? HospService { get => (string?)Elements["HospService"]; set => Elements["HospService"] = value; }

          [JsonPropertyOrder(32), SwaggerSchema("The hospital institution currently assigned to the patient. Both code and text may be sent when separated by a component delimiter (^).")]
          public string? HospInstitution { get => (string?)Elements["HospInstitution"]; set => Elements["HospInstitution"] = value; }

          [JsonPropertyOrder(33), SwaggerSchema("The patient dosage group. For example, A–ADULT, P1–PEDIATRIC (one to six months), P2–PEDIATRIC (six months to three years), etc.")]
          public string? DosageCategory { get => (string?)Elements["DosageCategory"]; set => Elements["DosageCategory"] = value; }

          [JsonIgnore]
          public virtual List<OrderBase> Orders { get => orders; set => orders = value; }

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