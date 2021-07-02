using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;

namespace IMMULIS
{
     public partial class IMMULIService : ServiceBase
     {
          public static CommPort ComPort;
          public static EventLog eventLog1 = new EventLog();
          // idleTimer periodically sends the next message in the queue, if any, and optionally polls the database for new requests.
          private static readonly System.Timers.Timer idleTimer = new System.Timers.Timer(Properties.Settings.Default.AutoSendInterval);
          // Initialize these timers to -1 to avoid triggering their respective Timeout events.
          public static CountdownTimer ContentTimer = new CountdownTimer(-1);
          public static CountdownTimer BusyTimer = new CountdownTimer(-1);
          public static CountdownTimer transTimer = new CountdownTimer(-1, TransactionTimedOut);
          public static CountdownTimer rcvTimer = new CountdownTimer(-1, ReceiptTimedOut);
          public static LISCommState commState = new LISCommState();
          public static int numNAK = 0;
          public static Queue<MessageBody> OutboundMessageQueue = new Queue<MessageBody>();
          public static int CurrentFrameCounter;
          public IMMULIService()
          {
               InitializeComponent();
               if (!EventLog.SourceExists("IMMULIS"))
               {
                    EventLog.CreateEventSource(
                        "IMMULIS", "IMMULog");
               }
               eventLog1.Source = "IMMULIS";
               eventLog1.Log = "IMMULog";
          }

          public void HandleEx(Exception ex)
          {
               if (ex is null)
               {
                    return;
               }
               string message = ex.Source + " - Error: " + ex.Message + "\n" + ex.TargetSite + "\n" + ex.StackTrace;
               eventLog1.WriteEntry(message);
          }

          protected override void OnStart(string[] args)
          {
               try
               {
                    AppendToLog("Service starting.");
                    // Set the serial port properties and try to open it.
                    ComPort = new CommPort(Properties.Settings.Default.SerialPortNum, Properties.Settings.Default.SerialPortBaudRate, (Parity)Properties.Settings.Default.SerialPortParity, Properties.Settings.Default.SerialPortDataBits, (StopBits)Properties.Settings.Default.SerialPortStopBits)
                    {
                         Handshake = Handshake.None,
                         ReadTimeout = 20,
                         WriteTimeout = 20
                    };
                    ComPort.Encoding = System.Text.Encoding.UTF8;
                    ComPort.DataReceived += ComPortDataReceived; // Set the handler for the DataReceived event.
                    ComPort.Open();
                    AppendToLog("Port opened.");
                    idleTimer.AutoReset = true;
                    idleTimer.Elapsed += new System.Timers.ElapsedEventHandler(IdleTime);
                    if (Properties.Settings.Default.AutoSendOrders == true){ idleTimer.Elapsed += WorklistTimedEvent; }
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
          }
          private void IdleTime(object o, System.Timers.ElapsedEventArgs elapsedEvent)
          {
               idleTimer.Stop();
               //check to see if commState is in IdleState and if there are any messages to be sent out
               commState.IdleCheck();
               idleTimer.Start();
          }
          public static void TransactionTimedOut(object sender, EventArgs e)
          {
               commState.TransTimeout();
          }
          public static void ReceiptTimedOut(object sender, EventArgs e)
          {
               commState.RcvTimeout();
          }
          protected override void OnStop()
          {
               try
               {
                    AppendToLog("Service stopping.");
                    ComPort.Close();
                    AppendToLog("Port closed.");
                    idleTimer.Stop();
                    idleTimer.Dispose();
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
          }
          

          public static void WorklistTimedEvent(object source, System.Timers.ElapsedEventArgs e)
          {
               /* If the outStringQueue is empty, SendPatientOrders for all pending orders
                * and their respective patient records to send them all to the IMMULITE.
                */
               // Stop the timer while any pending requests are sent.
               idleTimer.Stop();
               // Until the program is more stable, only add to the queue when it's empty.
               if (OutboundMessageQueue.Count == 0)
               {
                    SendPatientOrders("%", "ALL");
               }
               // Now that all pending requests have been sent, restart the timer.
               idleTimer.Start();
          }
          public static void AppendToLog(string txt)
          {    // To avoid write conflicts, let the CommPort handle the actual data logging.
               CommPort.AppendToLog(txt);
          }

          void ComPortDataReceived(object sender, DataReceivedArgs e) 
          {
               /* When new data is received, 
                * parse the message line-by-line.
                */
               CommPort sp = (CommPort)sender;
               string buffer = System.Text.Encoding.Default.GetString(e.Data); //store recieved data into a string
               bool timedOut = false;
               try
               {
                    idleTimer.Stop();
#if DEBUG
                    Stopwatch stopwatch = Stopwatch.StartNew(); // This stopwatch is an attempt at performance optimization. 
#endif
                    /* There are a few messages that won't end in a NewLine,
                     * so we have to read one character at a time until we run out of them.
                     */
                    do
                    { // Read one char at a time until the ReadChar times out.
                         try
                         {
                              buffer += (char)sp.ReadChar();
                         }
                         catch (Exception)
                         {
                              timedOut = true;
#if DEBUG
                              stopwatch.Stop();
#endif
                         }
                    } while (timedOut == false);
#if DEBUG
                    AppendToLog($"Elapsed port read time: {stopwatch.ElapsedMilliseconds}");
#endif
                    AppendToLog($"In: \t{buffer}");
                    // update state based on recieved message
                    commState.RcvInput(buffer);
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
                    throw;
               }
          }



          // CurrentMessage is used to store incoming messages one frame at a time.
          public static MessageBody CurrentMessage = new MessageBody();

          private static bool bInterFrame = false;
          private static string intermediateFrame;
          public static void ParseMessageLine(string messageLine)
          {
               // Parse each line of the incoming message as it's received.
               // This is done by assembling the CurrentMessage (a MessageBody) with Header, Patient, Order, Result, etc., records.
               // Receiving a Header record triggers instantiation of a new MessageBody.
               // As each subsequent component is received, add it to the CurrentMessage.
               // The message will be processed by the CommPortDataReceived method when the EOT signal is received.
               if (messageLine.Length < 5)
               {
                    AppendToLog($"Invalid message: {messageLine}");
                    return;
               }
               // Check if messageLine is an intermediate frame
               int position = messageLine.IndexOf(Constants.ETB);
               if (position >= 0)
               {
                    bInterFrame = true;
                    if (intermediateFrame.Length > 2)
                    {
                         intermediateFrame += messageLine.Substring(2, position);
                    }
                    else
                    {
                         intermediateFrame += messageLine.Substring(0, position);
                    }
                    return;
               }
               else
               {
                    if (bInterFrame)
                    {
                         intermediateFrame += messageLine.Substring(2, position);
                         messageLine = intermediateFrame;
                         intermediateFrame = "";
                         bInterFrame = false;
                    }
               }
               switch (messageLine.Substring(2, 1))
               {
                    case "H": // New message header.
                         CurrentMessage = new MessageBody(messageLine);
                         break;

                    case "P": // Patient record.
                         CurrentMessage.Patients.Add(new Patient(messageLine));
                         break;

                    case "O": // Order record.
                         CurrentMessage.Patients[CurrentMessage.Patients.Count() - 1].Orders.Add(new Order(messageLine));
                         break;

                    case "R": // Result record.
                         CurrentMessage.Patients[CurrentMessage.Patients.Count - 1].Orders[CurrentMessage.Patients[CurrentMessage.Patients.Count - 1].Orders.Count - 1].Results.Add(new Result(messageLine));
                         break;

                    case "L": // Terminator record.
                         if (messageLine.Substring(5,1) == Constants.CR)
                         {
                              CurrentMessage.Terminator = 'N';
                         }
                         else
                         {
                              CurrentMessage.Terminator = messageLine.ToCharArray()[5];
                         }
                         break;

                    case "Q": // Host Query record.
                         CurrentMessage.Queries.Add(new Query(messageLine));
                         break;

                    default:
                         break;
               }
          }

          private static int SendPatientOrders(string SampleNumber, string testID)
          {
                //true = patient orders being sent in response to a query
                //false = patient orders NOT being sent in response to a query
               bool isQuery = true;
               // If no SampleNumber is provided, use the % wildcard to get all pending orders.
               if (SampleNumber.Length == 0)
               {
                    SampleNumber = "%";
               }
               if (SampleNumber == "%")
               {
                    isQuery = false;
               }
               // Query the database for [P]atient and [O]rder records for the sample.
               using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
               {
                    conn.Open();
                    //AppendToLog("Database connection open.");
                    int orderCount;
                    using (SqlCommand sqlCommand = conn.CreateCommand())
                    { // Check to see how many orders are pending for the sample.
                         sqlCommand.CommandText = "SELECT COUNT(IMM_RequestOrders.ReqOrderID) AS OrderCount FROM IMM_RequestOrders WHERE (IMM_RequestOrders.Sample_Number LIKE @SampleNumber) AND (IMM_RequestOrders.PendingSending = 1)";
                         _ = sqlCommand.Parameters.AddWithValue("@SampleNumber", SampleNumber);
                         orderCount = (int)sqlCommand.ExecuteScalar();
                    }

                    if (orderCount == 0)
                    {    // No pending orders. 
                         if (isQuery == true)
                         {
                              // Reply with a "no information available from last query" message (terminator = "I")
                              AppendToLog($"No information available from last query (sample number: {SampleNumber})");
                              MessageBody messageBody = new MessageBody
                              {
                                   Terminator = 'I'
                              };
                              ProcessMessage(messageBody);
                         }
                         // Exit function.
                         return orderCount;
                    }
                    MessageBody response = new MessageBody();
                    int patientCount;
                    using (SqlCommand sqlCommand = conn.CreateCommand())
                    { // Check to see how many patients are associated with the sample.
                         sqlCommand.CommandText = "SELECT COUNT(RequestPID) AS PatientCount FROM (SELECT RequestPID FROM vwPendingRequests WHERE (Sample_Number LIKE @SampleNumber) GROUP BY RequestPID) AS tmp";
                         _ = sqlCommand.Parameters.AddWithValue("@SampleNumber", SampleNumber);
                         patientCount = (int)sqlCommand.ExecuteScalar();
                    }
                    
                    string sqlMainQuery = "SELECT * FROM vwPendingRequests WHERE Patient_Name LIKE @Patient_Name AND  Test_ID LIKE @Test_ID AND Sample_Number LIKE @Sample_Number;";
                    string sqlPatientQuery = "SELECT Patient_ID, Patient_Name, BirthDate, Patient_Sex FROM vwPendingRequests WHERE (Sample_Number LIKE @Sample_Number) AND (Test_ID LIKE @Test_ID) GROUP BY Patient_ID, Patient_Name, BirthDate, Patient_Sex";

                    
                    using (SqlCommand command = conn.CreateCommand())
                    { // Add Patients based on their sample number and tests pending
                         command.CommandText = sqlPatientQuery;
                         command.Parameters.AddWithValue("@Sample_Number", SampleNumber);
                         if (testID == "ALL")
                         {
                              testID = "%";
                         }
                         command.Parameters.AddWithValue("@Test_ID", testID);
                         SqlDataReader reader = command.ExecuteReader();
                         if (reader.HasRows == false)
                         { // No patients to add
                              return 0;
                         }
                         else
                         {
                              while (reader.Read())
                              { // Read through retrieved information from database and fill response with patients from that info
                                   response.Patients.Add(new Patient($"|{response.Patients.Count + 1}|{reader["Patient_ID"]}|||{reader["Patient_Name"]}||{reader["BirthDate"]}|{reader["Patient_Sex"]}||||||||||||||||||||||||||"));
                              }
                         }
                         
                         reader.Close();
                    }
                    foreach (var patient in response.Patients)
                    { // go through added patients in response and give them their corresponding orders from the database based on Patient_Name, Sample_Number, and Test_ID
                      // then update database that those orders are sent
                         string requestID = "";
                         using (SqlCommand cmmand = conn.CreateCommand())
                         {
                              cmmand.CommandText = sqlMainQuery;
                              cmmand.Parameters.AddWithValue("@Patient_Name", patient.Elements["Patient Name"]);
                              cmmand.Parameters.AddWithValue("@Sample_Number", SampleNumber);
                              if (testID == "ALL")
                              {
                                   testID = "%";
                              }
                              cmmand.Parameters.AddWithValue("@Test_ID", testID);
                              SqlDataReader reder = cmmand.ExecuteReader();
                              while (reder.Read())
                              { // add orders requested from the database to the patient
                                   patient.Orders.Add(new Order($"|{patient.Orders.Count + 1}|{reder["Sample_Number"]}||{reder["Test_ID"]}|{reder["Priority"]}|{reder["Order_DateTime"]}|{reder["Collection_DateTime"]}|||||||||||{reder["RequestPID"]}||||||||||||"));
                                   requestID = reder["RequestPID"].ToString();
                              }
                              reder.Close();
                         }
                         using (SqlCommand command = conn.CreateCommand())
                         { // update database that the orders just added to the current patient in the forloop are now sent
                              command.CommandText = "UPDATE IMM_RequestOrders SET PendingSending = 0 WHERE RequestPID = @RequestID";
                              command.Parameters.AddWithValue("@RequestID", requestID);
                              command.ExecuteNonQuery();
                         }
                    }
                    
                    if (isQuery == true)
                    {
                         response.Terminator = 'F';
                    }
                    else
                    {
                         response.Terminator = 'N';
                    }
                    ProcessMessage(response);
                    return orderCount;
               }
          }

          private static void ProcessQuery(Query query)
          {
               // Extract Sample Number from the query string.
               string SampleNumber = query.Elements["Starting Range"].Trim('^');
               string testID = query.Elements["Test ID"];
               // SendPatientOrders for the requested SampleNumber/testID.
               _ = SendPatientOrders(SampleNumber, testID);
          }

          public static void ProcessMessage(MessageBody message)
          {
               /* The message is complete. Deal with it. */
#if DEBUG
               AppendToLog("Processing message.");
#endif
               if (message == null)
               {
                    AppendToLog("MessageBody is null!");
                    return;
               }
               /* First, check to see whether the LIS is sending or receiving the message.
                * Do this by comparing the [Receiver ID] and [Sender ID] header fields.
                * The one that matches the value defined in the Settings file will tell us
                * how to handle the rest of the message.
                */
               string[] headerFields = message.MessageHeader.Split('|');
               if (headerFields[4] == Properties.Settings.Default.LIS_ID)
               {
                    // Message is outgoing. Queue it up to be sent to the IMMULITE.
#if DEBUG
                    AppendToLog("Outgoing message, adding to queue...");
#endif
                    OutboundMessageQueue.Enqueue(message);

                    return;
               }
               else if (headerFields[4] == Properties.Settings.Default.ReceiverID)
               {     
                    // Message is incoming. Add the contents to the appropriate database tables.
                    /* For our purposes, there are two primary incoming message types
                     * (from the IMMULITE to the LIS):
                     * 1. [Q]uery messages, and
                     * 2. [P]atient-[O]rder-[R]esult messages.
                     */
                    if (message.Queries.Count > 0) // This one is a query.
                    {
                         foreach (var query in message.Queries)
                         {
                              AppendToLog("Processing query.");
                              ProcessQuery(query);
                         }
                    }
                    else
                    {
                         int pID; // patient ID
                         int oID; // order ID
#if DEBUG 
                         AppendToLog("Connecting to database.");
#endif
                         // This is where we have to connect to the database.
                         using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
                         {
                              conn.Open();
#if DEBUG 
                              AppendToLog("Database connection open.");
#endif
                              foreach (var patient in message.Patients)
                              {
                                   /* * Patient fields: * *
                                    *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                                    *  These fields may or may not actually be functional.
                                    * 0 Record Type
                                    * 1 Sequence # Definition
                                    * 2 Practice Assigned Patient ID
                                    * 3 Laboratory Assigned Patient ID  =/=
                                    * 4 Patient ID  =/=
                                    * 5 Patient Name
                                    * 6 Mother's Maiden Name  =/=
                                    * 7 Birthdate
                                    * 8 Patient's Sex
                                    * 9 Patient Race-Ethnic Origin  =/=
                                    *10 Patient's Address  =/=
                                    *11 Reserved Field  =/=
                                    *12 Patient's Phone#  =/=
                                    *13 Attending Physician ID  =/=
                                    *14 Special Field 1  =/=
                                    *15 Special Field 2  =/=
                                    *16 Patient Height  =/=
                                    *17 Patient Weight  =/=
                                    *18 Patients Known or Suspected Diagnosis  =/=
                                    *19 Patient active medications  =/=
                                    *20 Patients Diet  =/=
                                    *21 Practice Field #1  =/=
                                    *22 Practice Field #2  =/=
                                    *23 Admission and Discharge Dates  =/=
                                    *24 Admission Status  =/=
                                    *25 Location  =/=
                                    *26 Nature of Alternative Diagnostic Code and Classification  =/=
                                    *27 Alternative Diagnostic Code and Classification  =/=
                                    *28 Patient Religion  =/=
                                    *29 Marital Status  =/=
                                    *30 Isolation Status  =/=
                                    *31 Language  =/=
                                    *32 Hospital Service  =/=
                                    *33 Hospital Institution  =/=
                                    *34 Dosage Category  =/=
                                    */
                                   AppendToLog("Adding patient.");
                                   // Transmit a patient record to the IMM_Patients table,
                                   // grab the row ID, and use it to add order records to the IMM_Orders table.
                                   using (SqlCommand command = new SqlCommand("spIMMPatient", conn))
                                   {
                                        command.CommandType = CommandType.StoredProcedure;
                                        _ = command.Parameters.AddWithValue("@Patient_ID", patient.Elements["Patient ID"]);
                                        _ = command.Parameters.AddWithValue("@Patient_Name", patient.Elements["Patient Name"]);
                                        _ = command.Parameters.AddWithValue("@Patient_DOB", patient.Elements["BirthDate"]);
                                        _ = command.Parameters.AddWithValue("@Patient_Sex", patient.Elements["Patient Sex"]);
                                        _ = command.Parameters.AddWithValue("@Physician_Name", patient.Elements["Attending Physician ID"]);
                                        SqlParameter sqlOutParameter = new SqlParameter("@ID", SqlDbType.Int)
                                        {
                                             Direction = ParameterDirection.Output
                                        }; 
                                        _ = command.Parameters.Add(sqlOutParameter);
                                        _ = command.ExecuteNonQuery();
                                        pID = int.Parse(command.Parameters["@ID"].Value.ToString());
                                   }
                                   foreach (var order in patient.Orders)
                                   {
                                        /*
                                        *  * Order Fields: * *
                                        *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                                        *  These fields may or may not actually be functional.
                                        *	0	Record Type (O)
                                        *	1	Sequence#
                                        *	2	Specimen ID (Accession#)
                                        *	3	Instrument Specimen ID
                                        *	4	Universal Test ID
                                        *	5	Priority
                                        *	6	Order Date/Time
                                        *	7	Collection Date/Time
                                        *	8	Collection End Time
                                        *	9	Collection Volume
                                        *	10	Collector ID
                                        *	11	Action Code
                                        *	12	Danger Code
                                        *	13	Relevant Clinical Info
                                        *	14	Date/Time Specimen Received
                                        *	15	Specimen Descriptor,Specimen Type,Specimen Source
                                        *	16	Ordering Physician
                                        *	17	Physician's Telephone Number
                                        *	18	User Field No.1
                                        *	19	User Field No.2
                                        *	20	Lab Field No.1
                                        *	21	Lab Field No.2
                                        *	22	Date/Time results reported or last modified
                                        *	23	Instrument Charge to Computer System
                                        *	24	Instrument Section ID
                                        *	25	Report Types
                                        *	26	Reserved Field
                                        *	27	Location or ward of Specimen Collection
                                        *	28	Nosocomial Infection Flag
                                        *	29	Specimen Service
                                        *	30	Specimen Institution
                                         */
                                        AppendToLog("Adding order.");
                                        using (SqlCommand command = new SqlCommand("spIMMOrder", conn))
                                        {
                                             // fillSqlCommand with information about the order
                                             command.CommandType = CommandType.StoredProcedure;
                                             _ = command.Parameters.AddWithValue("@Patient_ID", pID);
                                             _ = command.Parameters.AddWithValue("@Sample_Number", order.Elements["Specimen ID (Accession#)"]);
                                             _ = command.Parameters.AddWithValue("@Test_Code", order.Elements["Universal Test ID"]);
                                             _ = command.Parameters.AddWithValue("@Priority", order.Elements["Priority"]);
                                             _ = command.Parameters.AddWithValue("@Order_DateTime", order.Elements["Order Date/Time"]);
                                             _ = command.Parameters.AddWithValue("@Collection_DateTime", order.Elements["Collection Date/Time"]);
                                             SqlParameter sqlOutParameter = new SqlParameter("@ID", SqlDbType.Int)
                                             {
                                                  Direction = ParameterDirection.Output
                                             };
                                             _ = command.Parameters.Add(sqlOutParameter);
                                             _ = command.ExecuteNonQuery();
                                             oID = int.Parse(command.Parameters["@ID"].Value.ToString());
                                        }
                                        // Use the row ID from each of those order records to add
                                        // result records to the IMM_Results table for each Patient.Order.Result in the message.
                                        foreach (var result in order.Results)
                                        {
                                             /* * Result Fields: * *
                                             *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                                             *  These fields may or may not actually be functional.
                                             *	0	Record Type (R)
                                             *	1	Sequence #
                                             *	2	Universal Test ID
                                             *	3	Data (result)
                                             *	4	Units
                                             *	5	ReferenceRanges
                                             *	6	Result abnormal flags
                                             *	7	Nature of Abnormality Testing
                                             *	8	Result Status
                                             *	9	Date of change in instruments normal values or units
                                             *	10	Operator ID
                                             *	11	Date\Time Test Started
                                             *	12	Date\Time Test Completed
                                             *	13	Instrument ID
                                              */
                                             AppendToLog("Adding result.");
                                             using (SqlCommand command = new SqlCommand("spIMMResult", conn))
                                             {
                                                  // fill SqlCommand with information about the result
                                                  command.CommandType = CommandType.StoredProcedure;
                                                  _ = command.Parameters.AddWithValue("@Order_ID", oID);
                                                  _ = command.Parameters.AddWithValue("@Test_Code", result.Elements["Universal Test ID"]);
                                                  _ = command.Parameters.AddWithValue("@ResultValue", result.Elements["Data (result)"]);
                                                  _ = command.Parameters.AddWithValue("@ResultUnits", result.Elements["Units"]);
                                                  _ = command.Parameters.AddWithValue("@RefRanges", result.Elements["Reference Ranges"]);
                                                  _ = command.Parameters.AddWithValue("@AbnormalFlags", result.Elements["Result abnormal flags"]);
                                                  _ = command.Parameters.AddWithValue("@AbnormalityTesting", result.Elements["Nature of Abnormality Testing"]);
                                                  _ = command.Parameters.AddWithValue("@ResultStatus", result.Elements["Result Status"]);
                                                  _ = command.Parameters.AddWithValue("@Operator", result.Elements["Operator ID"]);
                                                  // Format datetime information of the result
                                                  string dtStart = result.Elements["Date/Time Test Started"];
                                                  DateTime start = new DateTime(Convert.ToInt32(dtStart.Substring(0, 4)), Convert.ToInt32(dtStart.Substring(4, 2)), Convert.ToInt32(dtStart.Substring(6, 2)), Convert.ToInt32(dtStart.Substring(8, 2)), Convert.ToInt32(dtStart.Substring(10, 2)), Convert.ToInt32(dtStart.Substring(12, 2)));
                                                  _ = command.Parameters.AddWithValue("@TestStarted", start);
                                                  string dtEnd = result.Elements["Date/Time Test Completed"];
                                                  DateTime end = new DateTime(Convert.ToInt32(dtEnd.Substring(0, 4)), Convert.ToInt32(dtEnd.Substring(4, 2)), Convert.ToInt32(dtEnd.Substring(6, 2)), Convert.ToInt32(dtEnd.Substring(8, 2)), Convert.ToInt32(dtEnd.Substring(10, 2)), Convert.ToInt32(dtEnd.Substring(12, 2)));
                                                  _ = command.Parameters.AddWithValue("@TestCompleted", end);
                                                  // trim ETX and CR
                                                  char[] trimmings = { '\x03', '\x0D' };
                                                  _ = command.Parameters.AddWithValue("@Instrument_ID", result.Elements["Instrument ID"].Trim(trimmings));
                                                  //execute sqlcommand
                                                  _ = command.ExecuteNonQuery();
                                             }
                                        }
                                   }
                              }
                         }
                    }
               }
               else
               {
                    // This message is invalid!
                    AppendToLog($"Invalid message received! {message.MessageHeader}");
                    throw new ArgumentException();
               }
          }

          public static string CHKSum(string message)
          {
               // This function returns the checksum for the data string passed to it.
               // If I've done it right, the checksum is calculated by binary 8-bit addition of all included characters
               // with the 8th or parity bit assumed zero. Carries beyond the 8th bit are lost. The 8-bit result is
               // converted into two printable ASCII Hex characters ranging from 00 to FF, which are then inserted into
               // the data stream. Hex alpha characters are always uppercase.

               string checkSum;
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

          public class MessageBody
          {
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
               public List<string> FrameList = new List<string>();
               private int FrameCounter { get; set; }
               public List<Patient> Patients = new List<Patient>();
               public List<Query> Queries = new List<Query>();

              /* * Message Terminator Record Definitions: *
               * 
               *  * Record Types Definition: *
               * L = Terminator Record                          |   Supported
               * ----------------------------------------------------------------------
               *  * Termination Codes: *
               * N = Normal Termination                         |   Supported
               * T = Sender Aborted                             |   Not Supported
               * R = Reciever Aborted                           |   Not Supported
               * E = Unknown System Error                       |   Not Supported
               * Q = Error in last request for information      |   Required With Query
               * I = No information available from last query   |   Required With Query
               * F = Last request for information processed     |   Required With Query
               */
               public char Terminator { get; set; }

               public string TerminationMessage
               {
                    get; set;
               }

               /* * Header fields:**
                *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                *  These fields may or may not actually be functional.
                * 0 Record Type
                * 1 Delimiter Def.
                * 2 Message Control ID  =/=
                * 3 Password
                * 4 Sending systems company name -OR- Sender ID
                * 5 Sending Systems address
                * 6 Reserved  =/=
                * 7 Senders Phone#
                * 8 Communication parameters
                * 9 Receiver ID
                *10 Comments/special instructions  =/=
                *11 Processing ID
                *12 Version#
                *13 Message Date + Time
                */
               public Dictionary<string, string> Elements = new Dictionary<string, string>();

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
                    Elements["Message Date + Time"] = inArray[13];
               }

               // returns header info as a string formatted according to the Immulite/LIS Manual
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
                    output += CHKSum(output) + Constants.CR + Constants.LF;
                    return output;
               }

               public MessageBody()
               {
                    string dateString;
                    DateTime dateTime = DateTime.Now;
                    //formats the date
                    dateString = dateTime.Year.ToString() + dateTime.Month.ToString("D2") + dateTime.Day.ToString("D2");
                    dateString += dateTime.Hour.ToString("D2") + dateTime.Minute.ToString("D2") + dateTime.Second.ToString("D2");
                    string header = Constants.STX + $"1H|\\^&||{Properties.Settings.Default.LIS_Password}|{Properties.Settings.Default.LIS_ID}|{Properties.Settings.Default.SenderAddress}";
                    header += $"||{Properties.Settings.Default.SenderPhone}|N81|{Properties.Settings.Default.ReceiverID}||P|1|{dateString}";
                    MessageHeader = header;
               }

               public MessageBody(string messageHeader)
               {
                    MessageHeader = messageHeader;
               }

               private bool isReady = false; // Indicates whether message has been processed with PrepareToSend().

               /* The FrameCounter starts at 1 for the MessageHeader. 
                * For each line of the message ("frame") after that, increment the FrameCounter by 1.
                * When it reaches 7, the next frame is 0.
                */
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
                
                /* adds InputString to framelist.
                 * splits in to two if necessary.
                 */
               private void FrameMessage(string InputString)
               {
                    if (InputString.Length > 240) //240 chars max length of a message
                    { //split InputString into two messages
                         string firstString = InputString.Substring(0, 235); //take first 235 chars because 5 chars are needed for the message information
                         // Add ETB because we know there will be another frame after this
                         firstString += Constants.ETB;                      
                         firstString += CHKSum(firstString);
                         firstString += Constants.CR + Constants.LF;
                         int iLength = InputString.Length - firstString.Length;
                         string nextString = InputString.Substring(235, iLength);
                         FrameList.Add(firstString);
                         FrameMessage(nextString); // recursive call for second half of string
                    }
                    else
                    {
                         FrameList.Add(InputString); // TODO: Recalculate checksum for last frame of multi-frame messages.
                    }
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
                    string term = Constants.STX + IncrementFrameCount().ToString() + "L|1|" + Terminator + Constants.CR + Constants.ETX;
                    term += CHKSum(term) + Constants.CR + Constants.LF;
                    TerminationMessage = term;
                    FrameMessage(TerminationMessage);
                    isReady = true;
               }
          }

          public class Result
          {
               public string ResultMessage
               {
                    get => GetResultString();
                    set => SetResultString(value);
               }

               public Result(string resultMessage)
               {
                    SetResultString(resultMessage);
               }

               public Dictionary<string, string> Elements = new Dictionary<string, string>();

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
               
                // returns result info as a string formatted according to the Immulite/LIS Manual
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
                    output += CHKSum(output) + Constants.CR + Constants.LF;
                    return output;
               }
          }

          public class Order
          {
               public string OrderMessage
               {
                    get
                    {
                         return GetOrderString();
                    }
                    set
                    {
                         SetOrderString(value);
                    }
               }

               public List<Result> Results = new List<Result>();

               public Order(string orderMessage)
               {
                    SetOrderString(orderMessage);
               }

               public Order()
               {
                    SetOrderString("O||||||||||||||||||||||||||||||");
               }

               public Dictionary<string, string> Elements = new Dictionary<string, string>();

               private void SetOrderString(string input)
               {
                    string[] inArray = input.Split('|');
                    if (inArray.Length < 31)
                    {
                         // Invalid number of elements.
                         throw new Exception("Invalid number of elements in order record string.");
                    }
                    Elements["FrameNumber"] = inArray[0];
                    Elements["Sequence#"] = inArray[1];
                    Elements["Specimen ID (Accession#)"] = inArray[2];
                    Elements["Instrument Specimen ID"] = inArray[3];
                    Elements["Universal Test ID"] = inArray[4];
                    Elements["Priority"] = inArray[5];
                    Elements["Order Date/Time"] = inArray[6];
                    Elements["Collection Date/Time"] = inArray[7];
                    Elements["Collection End Time"] = inArray[8];
                    Elements["Collection Volume"] = inArray[9];
                    Elements["Collector ID"] = inArray[10];
                    Elements["Action Code"] = inArray[11];
                    Elements["Danger Code"] = inArray[12];
                    Elements["Relevant Clinical Info"] = inArray[13];
                    Elements["Date/Time Specimen Received"] = inArray[14];
                    Elements["Specimen Descriptor"] = inArray[15];
                    Elements["Ordering Physician"] = inArray[16];
                    Elements["Physician's Telephone Number"] = inArray[17];
                    Elements["User Field No.1"] = inArray[18];
                    Elements["User Field No.2"] = inArray[19];
                    Elements["Lab Field No.1"] = inArray[20];
                    Elements["Lab Field No.2"] = inArray[21];
                    Elements["Date/Time results reported or last modified"] = inArray[22];
                    Elements["Instrument Charge to Computer System"] = inArray[23];
                    Elements["Instrument Section ID"] = inArray[24];
                    Elements["Report Types"] = inArray[25];
                    Elements["Reserved Field"] = inArray[26];
                    Elements["Location or ward of Specimen Collection"] = inArray[27];
                    Elements["Nosocomial Infection Flag"] = inArray[28];
                    Elements["Specimen Service"] = inArray[29];
                    Elements["Specimen Institution"] = inArray[30];
               }

            // returns order info as a string formatted according to the Immulite/LIS Manual
            private string GetOrderString()
               {
                    // Anything missing should be added as an empty string.
                    string[] elementArray = { "FrameNumber", "Sequence#", "Specimen ID (Accession#)", "Instrument Specimen ID", "Universal Test ID", "Priority", "Order Date/Time", "Collection Date/Time", "Collection End Time", "Collection Volume", "Collector ID", "Action Code", "Danger Code", "Relevant Clinical Info", "Date/Time Specimen Received", "Specimen Descriptor", "Ordering Physician", "Physician's Telephone Number", "User Field No.1", "User Field No.2", "Lab Field No.1", "Lab Field No.2", "Date/Time results reported or last modified", "Instrument Charge to Computer System", "Instrument Section ID", "Report Types", "Reserved Field", "Location or ward of Specimen Collection", "Nosocomial Infection Flag", "Specimen Service", "Specimen Institution" };
                    foreach (var item in elementArray)
                    {
                         if (!Elements.ContainsKey(item))
                         {
                              Elements.Add(item, "");
                         }
                    }
                    string output = Constants.STX + Elements["FrameNumber"].Trim('O') + "O|";
                    // Concatenate the Dictionary values and return the string.
                    output += Elements["Sequence#"] + "|";
                    output += Elements["Specimen ID (Accession#)"] + "|";
                    output += Elements["Instrument Specimen ID"] + "|";
                    output += Elements["Universal Test ID"] + "|";
                    output += Elements["Priority"] + "|";
                    output += Elements["Order Date/Time"] + "|";
                    output += Elements["Collection Date/Time"] + "|";
                    output += Elements["Collection End Time"] + "|";
                    output += Elements["Collection Volume"] + "|";
                    output += Elements["Collector ID"] + "|";
                    output += Elements["Action Code"] + "|";
                    output += Elements["Danger Code"] + "|";
                    output += Elements["Relevant Clinical Info"] + "|";
                    output += Elements["Date/Time Specimen Received"] + "|";
                    output += Elements["Specimen Descriptor"] + "|";
                    output += Elements["Ordering Physician"] + "|";
                    output += Elements["Physician's Telephone Number"] + "|";
                    output += Elements["User Field No.1"] + "|";
                    output += Elements["User Field No.2"] + "|";
                    output += Elements["Lab Field No.1"] + "|";
                    output += Elements["Lab Field No.2"] + "|";
                    output += Elements["Date/Time results reported or last modified"] + "|";
                    output += Elements["Instrument Charge to Computer System"] + "|";
                    output += Elements["Instrument Section ID"] + "|";
                    output += Elements["Report Types"] + "|";
                    output += Elements["Reserved Field"] + "|";
                    output += Elements["Location or ward of Specimen Collection"] + "|";
                    output += Elements["Nosocomial Infection Flag"] + "|";
                    output += Elements["Specimen Service"] + "|";
                    output += Elements["Specimen Institution"] + Constants.CR + Constants.ETX;
                    output += CHKSum(output) + Constants.CR + Constants.LF;
                    return output;
               }
          }

          public class Patient
          {
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

               public List<Order> Orders = new List<Order>();
               public Dictionary<string, string> Elements = new Dictionary<string, string>();

               public Patient(string patientMessage)
               {
                    SetPatientString(patientMessage);
               }

               public Patient()
               {
                    SetPatientString("|1|||||||||||||||||||||||||||||||||");
               }

            /* Fills Elements dictionary with info from input string.
             * Look at constructors for example of input.
            */
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

               // returns patient info as a string formatted according to the Immulite/LIS Manual
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
                    output += CHKSum(output) + Constants.CR + Constants.LF;
                    return output;
               }
          }

          public class Query
          {
               public string QueryMessage
               {
                    get
                    {
                         return GetQueryString();
                    }
                    set
                    {
                         SetQueryString(value);
                    }
               }

               public Query(string queryMessage)
               {
                    SetQueryString(queryMessage);
               }

               public Query()
               {    // Unused, since the LIS doesn't query the IMMULITE.
                    SetQueryString("2Q|1|||ALL||||||||O");
               }

               public Dictionary<string, string> Elements = new Dictionary<string, string>();

               /* Fills Elements dictionary with info from input string.
                * Look at constructors for example of input.
                */
               private void SetQueryString(string input)
               {
                    string[] inArray = input.Split('|');
                    if (inArray.Length < 13)
                    {
                         // Invalid number of elements.
                         throw new Exception($"Invalid number of elements in query record string. Expected: 13 \tFound: {inArray.Length} \tString: \n{input}");
                    }
                    Elements["FrameNumber"] = inArray[0];
                    Elements["Sequence #"] = inArray[1];
                    Elements["Starting Range"] = inArray[2];
                    Elements["Ending Range"] = inArray[3];
                    Elements["Test ID"] = inArray[4];
                    Elements["Request Time Limits"] = inArray[5];
                    Elements["Beginning request results date and time"] = inArray[6];
                    Elements["Ending request results date and time"] = inArray[7];
                    Elements["Physician name"] = inArray[8];
                    Elements["Physician Phone Number"] = inArray[9];
                    Elements["User Field 1"] = inArray[10];
                    Elements["User Field 2"] = inArray[11];
                    Elements["Status Codes"] = inArray[12];
               }

               // returns query info as a string formatted according to the Immulite/LIS Manual
               private string GetQueryString()
               {    // This method shouldn't actually be used, since the LIS shouldn't be sending any queries.
                    // Anything missing should be added as an empty string.
                    string[] elementArray = { "FrameNumber", "Sequence #", "Starting Range", "Ending Range", "Test ID", "Request Time Limits", "Beginning request results date and time", "Ending request results date and time", "Physician name", "Physician Phone Number", "User Field 1", "User Field 2", "Status Codes" };
                    foreach (var item in elementArray)
                    {
                         if (!Elements.ContainsKey(item))
                         {
                              Elements.Add(item, "");
                         }
                    }
                    string output = Constants.STX + Elements["FrameNumber"].Trim('Q') + "Q|";
                    // Concatenate the Dictionary values and return the string.
                    output += Elements["Sequence #"] + "|";
                    output += Elements["Starting Range"] + "|";
                    output += Elements["Ending Range"] + "|";
                    output += Elements["Test ID"] + "|";
                    output += Elements["Request Time Limits"] + "|";
                    output += Elements["Beginning request results date and time"] + "|";
                    output += Elements["Ending request results date and time"] + "|";
                    output += Elements["Physician name"] + "|";
                    output += Elements["Physician Phone Number"] + "|";
                    output += Elements["User Field 1"] + "|";
                    output += Elements["User Field 2"] + "|";
                    output += Elements["Status Codes"] + Constants.CR + Constants.ETX;
                    output += CHKSum(output) + Constants.CR + Constants.LF;
                    return output;
               }
          }
          
          public static class Constants
          {
               // These are the ANSI control codes used by the IMMULITE serial communications protocol.
               public const string ACK = "\x06"; //acknowledgement signal

               public const string NAK = "\x15"; //negative acknoledgement signal (didn't recieve it or can't recieve)
               public const string ENQ = "\x05"; //enquiry character (check if other side available)
               public const string EOT = "\x04"; //End of Transmission character
               public const string STX = "\x02"; //start of text (first char of message)
               public const string ETX = "\x03"; //end of text (signals end of the text portion of a message)
               public const string CR = "\x0D";  //carriage return (comes before ETX and CheckSum)
               public const string LF = "\x0A";  //line feed (marks end of frame following carriage return)
               public const string ETB = "\x17"; //used instead of ETX to mark intermediate frame
          }

          // <summary>Method invoked when service is started from a debugging console.</summary>
          internal void DebuggingRoutine(string[] args)
          {
               OnStart(args);
               Console.ReadLine();
               OnStop();
          }
     }
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

}