using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMMULIS
{
     /* Example of unidirectional message structure/hierarchy:
      * Header
      *   Patient
      *        Order
      *             Result
      *        Order
      *             Result
      *   Patient
      *        Order
      *             Result
      * Terminator
      */
     public class Message
     {
          
          public Dictionary<string, string> Elements = new Dictionary<string, string>();

          public List<string> FrameList = new List<string>();

          private bool isReady = false;

          public List<Patient> Patients = new List<Patient>();

          public List<Query> Queries = new List<Query>();

          private int FrameCounter { get; set; }

          public string MessageHeader
          {
               get
               {
                    return GetHeaderString();
               }
               set
               {
                    SetHeaderString(value);
               }
          }

          public string TerminationMessage
          {
               get; set;
          }

          public char Terminator { get; set; }

          private void FrameMessage(string InputString)
          {
               /* According to the ASTM E1381-95 standard, frames longer than 240 characters 
               *  -- 247 characters including frame overhead (<STX>[FrameNumber]...<ETX>[Checksum]<CR><LF>) --
               *  are sent as one or more intermediate frames followed by an end frame. 
               *  Shorter messages are sent as a single end frame.
               *  Intermediate frames use <ETB> in place of <ETX> to indicate that it is continued
               *  in the next frame. 
               *  This procedure splits long frames into intermediate frames if necessary
               *  before appending the checksum and <CR><LF> and adding the frame to the message FrameList.
               */
               if (InputString.Length > 243) // <STX> + FrameNumber + 240-character frame + <ETX> = 243
               {
                    string firstString = InputString.Substring(0, 242); // 242 to make room for the <ETB>
                    int firstStringLength = firstString.Length;
                    int iLength = InputString.Length - firstStringLength;
                    firstString += Constants.ETB;
                    firstString += ServiceMain.CHKSum(firstString);
                    firstString += Constants.CR + Constants.LF;
                    string nextString = InputString.Substring(firstStringLength, iLength); // The remainder of the string
                    FrameList.Add(firstString); // Add intermediate frame to list           // is passed to this function recursively
                    FrameMessage(nextString);                                               // to be added as its own frame(s)
               }
               else
               {
                    InputString += ServiceMain.CHKSum(InputString) + Constants.CR + Constants.LF; // Tag on the checksum and <CR><LF>
                    FrameList.Add(InputString); // Add the end frame
               }
          }

          private string GetHeaderString()
          {
               // Anything missing should be added as an empty string.
               string[] elementArray = { "FrameNumber", "Delimiter Def.", "Message Control ID", "Password", "Sending systems company name", "Sending systems address", "Reserved", "Senders phone #", "Communication parameters", "Receiver ID", "Comments/special instrucitons", "Processing ID", "Version #", "Message Date + Time" };
               foreach (var item in elementArray)
               {
                    if (!Elements.ContainsKey(item))
                    {
                         Elements.Add(item, "");
                    }
               }
               // Concatenate the Dictionary values and return the string.
               string output = Constants.STX + Elements["FrameNumber"] + "H|";
               output += Elements["Delimiter Def."] + "|";
               output += Elements["Message Control ID"] + "|";
               output += Elements["Password"] + "|";
               output += Elements["Sending systems company name"] + "|";
               output += Elements["Sending systems address"] + "|";
               output += Elements["Reserved"] + "|";
               output += Elements["Senders phone #"] + "|";
               output += Elements["Communication parameters"] + "|";
               output += Elements["Receiver ID"] + "|";
               output += Elements["Comments/special instrucitons"] + "|";
               output += Elements["Processing ID"] + "|";
               output += Elements["Version #"] + "|";
               output += Elements["Message Date + Time"] + Constants.CR + Constants.ETX;
               return output;
          }

          private int IncrementFrameCount()
          {
               if (FrameCounter == 7)
               {
                    FrameCounter = 0;
               }
               else
               {
                    FrameCounter += 1;
               }
               return FrameCounter;
          }

          public void PrepareToSend()
          {
               if (isReady)
               {
                    return;
               }
               // The header fields should already have been set to define the MessageHeader string.
               if (MessageHeader.Length == 0)
               {
                    throw new Exception("Missing header record string.");
               }
               FrameList.Clear();
               FrameMessage(MessageHeader);
               // Then process each patient and their orders, incrementing the frame number with each record.
               if (Patients.Count > 0)
               {
                    foreach (var patient in Patients)
                    {
                         patient.Elements["FrameNumber"] = IncrementFrameCount().ToString();
                         // Add the patient message to the FrameList.
                         FrameMessage(patient.PatientMessage);
                         // Do the same for each order message.
                         foreach (var order in patient.Orders)
                         {
                              order.Elements["FrameNumber"] = IncrementFrameCount().ToString();
                              FrameMessage(order.OrderMessage);
                         }
                    }
               }
               // Finally, don't forget the Terminator message.
               if (Terminator < 'E')
               {
                    Terminator = 'N';
               }
               TerminationMessage = Constants.STX + IncrementFrameCount().ToString() + "L|1|" + Terminator + Constants.CR + Constants.ETX;
               FrameMessage(TerminationMessage);
               isReady = true;
          }

          private void SetHeaderString(string input)
          {
               string[] inArray = input.Split('|');
               if (inArray.Length < 14)
               {
                    // Invalid number of elements.
                    throw new Exception($"Invalid number of elements in header record string. Expected: 14 \tFound: {inArray.Length} \tString: \n{input}");
               }
               FrameCounter = 1;
               Elements["FrameNumber"] = "1";
               Elements["Delimiter Def."] = inArray[1];
               Elements["Message Control ID"] = inArray[2];
               Elements["Password"] = inArray[3];
               Elements["Sending systems company name"] = inArray[4];
               Elements["Sending systems address"] = inArray[5];
               Elements["Reserved"] = inArray[6];
               Elements["Senders phone #"] = inArray[7];
               Elements["Communication parameters"] = inArray[8];
               Elements["Receiver ID"] = inArray[9];
               Elements["Comments/special instrucitons"] = inArray[10];
               Elements["Processing ID"] = inArray[11];
               Elements["Version #"] = inArray[12];
               if (inArray[13].Length < 14 && inArray[13].Length >= 8)
               {
                    // Date only.
                    Elements["Message Date + Time"] = inArray[13].Substring(0, 8);
               }
               else if (inArray[13].Length>=14)
               {
                    // Date and time.
                    Elements["Message Date + Time"] = inArray[13].Substring(0, 14);
               }
               else
               {
                    // Invalid datetime format.
                    throw new Exception($"Invalid datetime in header record string. Expected: 14 \tFound: {inArray[13]} \tString: \n{input}");
               }
               
          }

          //public Message()
          //{
          //     string dateString;
          //     DateTime dateTime = DateTime.Now;
          //     dateString = dateTime.Year.ToString() + dateTime.Month.ToString("D2") + dateTime.Day.ToString("D2");
          //     dateString += dateTime.Hour.ToString("D2") + dateTime.Minute.ToString("D2") + dateTime.Second.ToString("D2");
          //     string header = Constants.STX + $"1H|\\^&||{Properties.Settings.Default.LIS_Password}|{Properties.Settings.Default.LIS_ID}|{Properties.Settings.Default.SenderAddress}";
          //     header += $"||{Properties.Settings.Default.SenderPhone}|8N1|{Properties.Settings.Default.ReceiverID}||P|1|{dateString}";
          //     MessageHeader = header;
          //}

          public Message(string messageHeader)
          {
               MessageHeader = messageHeader;
          }

          public Message(CommFacilitator facilitator)
          {
               int stopbits;
               switch (facilitator.ComPort.StopBits)
               {
                    case System.IO.Ports.StopBits.One:
                         stopbits = 1;
                         break;
                    case System.IO.Ports.StopBits.Two:
                         stopbits = 2;
                         break;
                    default:
                         stopbits = 1;
                         break;
               }
               string parity;
               switch (facilitator.ComPort.Parity)
               {
                    case System.IO.Ports.Parity.Even:
                         parity = "E";
                         break;
                    case System.IO.Ports.Parity.Odd:
                         parity = "O";
                         break;
                    case System.IO.Ports.Parity.Mark:
                         parity = "M";
                         break;
                    case System.IO.Ports.Parity.Space:
                         parity = "S";
                         break;
                    default:
                         parity = "N";
                         break;
               }
               string dateString;
               DateTime dateTime = DateTime.Now;
               dateString = dateTime.Year.ToString() + dateTime.Month.ToString("D2") + dateTime.Day.ToString("D2");
               dateString += dateTime.Hour.ToString("D2") + dateTime.Minute.ToString("D2") + dateTime.Second.ToString("D2");
               string header = Constants.STX + $"1H|\\^&||{Properties.Settings.Default.LIS_Password}|{Properties.Settings.Default.LIS_ID}|{Properties.Settings.Default.SenderAddress}";
               header += $"||{Properties.Settings.Default.SenderPhone}|{facilitator.ComPort.DataBits}{parity}{stopbits}|{facilitator.receiver_id}||P|1|{dateString}";
               MessageHeader = header;
          }
     }
}