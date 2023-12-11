namespace UniversaLIS.Models
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
          public Dictionary<string, string> Elements { get; set; } = new Dictionary<string, string>();
          public enum MessageDirection
          {
               Inbound,
               Outbound
          }
          public MessageDirection Direction { get; set; }
          public List<string> FrameList { get; set; } = new List<string>();

          private bool isReady = false;

          public List<PatientBase> Patients { get; set; } = new List<PatientBase>();

          public List<Query> Queries { get; set; } = new List<Query>();

          private int FrameCounter { get; set; }

          private readonly int frameSize;

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

          public string? TerminationMessage
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
               *  CLSI-LIS1-A increased this frame size to 64,000 including frame overhead.
               *  The use_legacy_frame_size setting in config.yml is used to specify which size to use.
               */
               if (InputString.Length > frameSize + 3) // <STX> + FrameNumber + frame + <ETX>
               {
                    string firstString = InputString.Substring(0, frameSize + 2); // +2 to make room for the <ETB>
                    int firstStringLength = firstString.Length;
                    int iLength = InputString.Length - firstStringLength;
                    firstString += Constants.ETB;
                    firstString += CHKSum(firstString);
                    firstString += Constants.CR + Constants.LF;
                    string nextString = InputString.Substring(firstStringLength, iLength); // The remainder of the string
                    FrameList.Add(firstString); // Add intermediate frame to list           // is passed to this function recursively
                    FrameMessage(nextString);                                               // to be added as its own frame(s)
               }
               else
               {
                    InputString += CHKSum(InputString) + Constants.CR + Constants.LF; // Tag on the checksum and <CR><LF>
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

          public static string CHKSum(string message)
          {
               // This function returns the checksum for the data string passed to it.
               // If I've done it right, the checksum is calculated by binary 8-bit addition of all included characters
               // with the 8th or parity bit assumed zero. Carries beyond the 8th bit are lost. The 8-bit result is
               // converted into two printable ASCII Hex characters ranging from 00 to FF, which are then inserted into
               // the data stream. Hex alpha characters are always uppercase.

               string? checkSum;
               int ascSum, modVal;
               ascSum = 0;
               foreach (char c in message)
               {
                    if ((int)c != 2)
                    {    // Don't count any STX.
                         ascSum += (int)c;
                    }
               }
               modVal = ascSum % 256;
               checkSum = modVal.ToString("X");
               return checkSum.PadLeft(2, '0');
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
                    throw new InvalidOperationException("Missing MessageHeader string.");
               }
               FrameList.Clear();
               FrameMessage(MessageHeader);
               // Then process each patient and their orders, incrementing the frame number with each record.
               if (Patients.Count > 0)
               {
                    int pCount = 0;
                    foreach (var patient in Patients)
                    {
                         pCount += 1;
                         patient.Elements["FrameNumber"] = IncrementFrameCount().ToString();
                         // Assign proper Sequence Number to each patient.
                         patient.Elements["SequenceNumber"] = pCount.ToString();
                         // Add the patient message to the FrameList.
                         FrameMessage(patient.GetPatientMessage());
                         // Do the same for each order message.
                         int oCount = 0;
                         foreach (var order in patient.Orders)
                         {
                              oCount += 1;
                              order.Elements["FrameNumber"] = IncrementFrameCount().ToString();
                              // Assign proper Sequence Number to each order.
                              order.Elements["SequenceNumber"] = oCount.ToString();
                              FrameMessage(order.GetOrderMessage());
                              // If there are any result messages for the order, prepare those, too.
                              int rCount = 0;
                              foreach (var result in order.Results)
                              {
                                   rCount += 1;
                                   result.Elements["FrameNumber"] = IncrementFrameCount().ToString();
                                   // Assign proper Sequence Number to each order.
                                   result.Elements["SequenceNumber"] = rCount.ToString();
                                   FrameMessage(result.GetResultMessage());
                              }
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
                    throw new ArgumentException($"Invalid number of elements in header record string. Expected: 14 \tFound: {inArray.Length} \tString: \n{input}");
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
               else if (inArray[13].Length >= 14)
               {
                    // Date and time.
                    Elements["Message Date + Time"] = inArray[13].Substring(0, 14);
               }
               else
               {
                    // Invalid datetime format.
                    throw new ArgumentOutOfRangeException($"Invalid datetime in header record string. Expected: YYYYMMDDHHMMSS or YYYYMMDD \tFound: {inArray[13]} \tString: \n{input}");
               }

          }

          public Message(string messageHeader)
          {
               MessageHeader = messageHeader;
               Direction = MessageDirection.Inbound;
          }

          public Message(int frameSize, string? password, string? lisId, string? address, string? phone, string? portDetails, string? receiverId)
          {
               this.frameSize = frameSize;
               string dateString;
               DateTime dateTime = DateTime.Now;
               dateString = dateTime.Year.ToString() + dateTime.Month.ToString("D2") + dateTime.Day.ToString("D2");
               dateString += dateTime.Hour.ToString("D2") + dateTime.Minute.ToString("D2") + dateTime.Second.ToString("D2");
               string header = Constants.STX + $"1H|\\^&||{password}|{lisId}|{address}";
               header += $"||{phone}|{portDetails}|{receiverId}||P|1|{dateString}";
               MessageHeader = header;
               Direction = MessageDirection.Outbound;
          }

     }
}