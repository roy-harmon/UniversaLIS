using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using static UniversaLIS.UniversaLIService;
// TODO: Escape special characters in message fields, remove any delimiter characters within field contents.
// TODO: Consider field mappings when constructing SQL commands.
namespace UniversaLIS
{
     
     public class CommFacilitator
     {
          private bool bInterFrame = false;
          private readonly IPortAdapter ComPort;
          private readonly IPortSettings portSettings;
          internal UniversaLIService service;
          public CountdownTimer ContentTimer { get; set; } = new CountdownTimer(-1);

          public int CurrentFrameCounter { get; set; }

          public Message CurrentMessage { get; set; }

          private string? intermediateFrame;

          public int NumNAK { get; set; } = 0;

          public Queue<Message> OutboundMessageQueue { get; set; } = new Queue<Message>();

          public CountdownTimer RcvTimer { get; set; }

          public CountdownTimer TransTimer { get; set; }

          private readonly System.Timers.Timer idleTimer = new System.Timers.Timer();

          internal string? receiver_id;
          internal int frameSize;
          internal string? password;

          public CountdownTimer BusyTimer { get; set; } = new CountdownTimer(-1);
          public LisCommState CommState { get; set; }

          internal string GetPortDetails()
          {
               return portSettings.GetPortDetails();
          }
          public void Send(string messageText)
          {
               ComPort.Send(messageText);
          }

          // Use this string when setting up internal database connection functions.
          // readonly string connString = UniversaLIService.YamlSettings?.ServiceConfig?.SqlitePath!;

          public CommFacilitator(Serial serialSettings, UniversaLIService LIService)
          {
               service = LIService;
               portSettings = serialSettings;
               if (serialSettings.UseLegacyFrameSize)
               {
                    frameSize = 240;
               }
               else
               {
                    frameSize = 63993;
               }
               password = serialSettings.Password;
               try
               {
                    // Set the serial port properties and try to open it.
                    receiver_id = serialSettings.ReceiverId;
                    ComPort = new CommPort(serialSettings, this);
                    CommState = new LisCommState(this);
                    RcvTimer = new CountdownTimer(-1, ReceiptTimedOut);
                    TransTimer = new CountdownTimer(-1, TransactionTimedOut);
                    CurrentMessage = new Message(this);
                    // Set the handler for the DataReceived event.
                    ComPort.PortDataReceived += CommPortDataReceived!;
                    ComPort.Open();
                    AppendToLog($"Port opened: {serialSettings.Portname}");
                    idleTimer.AutoReset = true;
                    idleTimer.Elapsed += new System.Timers.ElapsedEventHandler(IdleTime);
                    if (serialSettings.AutoSendOrders > 0)
                    {
                         idleTimer.Elapsed += WorklistTimedEvent;
                         idleTimer.Interval = serialSettings.AutoSendOrders;
                    }
                    else
                    {
                         idleTimer.Interval = 10000;
                    }
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    service.HandleEx(ex);
                    throw;
               }
          }

          public CommFacilitator(Tcp tcpSettings, UniversaLIService LIService)
          {
               service = LIService;
               CommState = new LisCommState(this);
               RcvTimer = new CountdownTimer(-1, ReceiptTimedOut);
               TransTimer = new CountdownTimer(-1, TransactionTimedOut);
               portSettings = tcpSettings;
               if (tcpSettings.UseLegacyFrameSize)
               {
                    frameSize = 240;
               }
               else
               {
                    frameSize = 63993;
               }
               password = tcpSettings.Password;
               try
               {
                    // Set the port properties and try to open it.
                    receiver_id = tcpSettings.ReceiverId;
                    ComPort = new TcpPort(tcpSettings);
                    // Set the handler for the DataReceived event.
                    ComPort.PortDataReceived += CommPortDataReceived!;
                    CurrentMessage = new Message(this);
                    ComPort.Open();
                    AppendToLog($"Socket opened: {tcpSettings.Socket}");
                    idleTimer.AutoReset = true;
                    idleTimer.Elapsed += new System.Timers.ElapsedEventHandler(IdleTime);
                    if (tcpSettings.AutoSendOrders > 0)
                    {
                         idleTimer.Elapsed += WorklistTimedEvent;
                    }
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    service.HandleEx(ex);
                    throw;
               }
          }
          public void Close()
          {
               try
               {
                    idleTimer.Stop();
                    idleTimer.Dispose();
                    /* TODO: Update the request table so that any unsent messages will be sent next time the service runs.
                     * This may require adding an ID field to the Order class.
                     */
                    while (OutboundMessageQueue.Count > 0)
                    {
                         Message message = OutboundMessageQueue.Dequeue();

                         using (SqlConnection conn = new SqlConnection(UniversaLIService.YamlSettings?.ServiceConfig?.ConnectionString))
                         {
                              foreach (Patient patientItem in message.Patients)
                              {
                                   foreach (Order orderItem in patientItem.Orders)
                                   {
                                        using (SqlCommand command = conn.CreateCommand())
                                        {
                                             command.CommandText = "UPDATE IMM_RequestOrders SET PendingSending = 1 WHERE ReqOrderID = @OrderID";
                                             command.Parameters.AddWithValue("@OrderID", orderItem.OrderID);
                                             command.ExecuteNonQuery();
                                        }
                                   }
                              }
                         }
                    }
                    ComPort.Close();
                    AppendToLog($"Port closed: {ComPort.PortName}");
               }
               catch (Exception ex)
               {
                    service.HandleEx(ex);
                    throw;
               }
          }

          void CommPortDataReceived(object sender, EventArgs e)
          {
               /* When new data is received, 
                * parse the message line-by-line.
                */
               IPortAdapter port = (IPortAdapter)sender;
               StringBuilder buffer = new StringBuilder();
               try
               {
                    idleTimer.Stop();
#if DEBUG
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew(); // This stopwatch is an attempt at performance optimization. 
#endif
                    /* There are a few messages that won't end in a NewLine,
                     * so we have to read one character at a time until we run out of them.
                     */
                    // Read one char at a time until the ReadChar times out.
                    try
                    {
                         buffer.Append(port.ReadChars());
                    }
                    catch (Exception)
                    {
#if DEBUG
                         stopwatch.Stop();
#endif
                    }
#if DEBUG
                    UniversaLIService.AppendToLog($"Elapsed port read time: {stopwatch.ElapsedMilliseconds}");
#endif
                    UniversaLIService.AppendToLog($"In: \t{buffer}");
                    CommState.RcvInput(buffer.ToString());
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    service.HandleEx(ex);
                    throw;
               }
          }

          private void IdleTime(object? o, System.Timers.ElapsedEventArgs elapsedEvent)
          {
               idleTimer.Stop();
               CommState.IdleCheck();
               idleTimer.Start();
          }

          public void ParseMessageLine(string messageLine)
          {
               // Parse each line of the incoming message as it's received.
               // This is done by assembling the CurrentMessage (a MessageBody) with Header, Patient, Order, Result, etc., records.
               // Receiving a Header record triggers instantiation of a new MessageBody.
               // As each subsequent component is received, add it to the CurrentMessage.
               // The message will be processed by the CommPortDataReceived method when the EOT signal is received.
               if (messageLine.Length < 5)
               {
                    UniversaLIService.AppendToLog($"Invalid message: {messageLine}");
                    return;
               }
               int position = messageLine.IndexOf(Constants.ETB);
               if (position >= 0)
               {
                    bInterFrame = true;
                    if (intermediateFrame?.Length > 2)
                    {
                         intermediateFrame += messageLine.Substring(2, position);
                    }
                    else
                    {
                         intermediateFrame += messageLine.Substring(0, position);
                    }
                    return; // Don't process yet.
               }
               else
               {
                    if (bInterFrame) // If it's an intermediate frame, trim off the frame number and concatenate with previous frame.
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
                         CurrentMessage = new Message(messageLine);
                         break;

                    case "P": // Patient record.
                         CurrentMessage.Patients.Add(new Patient(messageLine));
                         break;

                    case "O": // Order record.
                         CurrentMessage.Patients[^1].Orders.Add(new Order(messageLine));
                         break;

                    case "R": // Result record.
                         CurrentMessage.Patients[^1].Orders[^1].Results.Add(new Result(messageLine));
                         break;

                    case "L": // Terminator record.
                         if (messageLine.Substring(5, 1) == Constants.CR)
                         {
                              CurrentMessage.Terminator = 'N';
                         }
                         else
                         {
                              CurrentMessage.Terminator = messageLine[5];
                         }
                         break;

                    case "Q": // Host Query record.
                         CurrentMessage.Queries.Add(new Query(messageLine));
                         break;

                    default:
                         break;
               }
          }

          public void ProcessMessage(Message message)
          {
               /* The message is complete. Deal with it. */
#if DEBUG
               UniversaLIService.AppendToLog("Processing message.");
#endif
               if (message == null)
               {
                    UniversaLIService.AppendToLog("MessageBody is null!");
                    return;
               }
               /* First, check to see whether the LIS is sending or receiving the message.
                * Do this by comparing the [Receiver ID] and [Sender ID] header fields.
                * The one that matches the value defined in the Settings file will tell us
                * how to handle the rest of the message.
                */
               string[] headerFields = message.MessageHeader.Split('|');
               if (headerFields[4] == UniversaLIService.YamlSettings?.ServiceConfig?.LisId)
               {
                    // Message is outgoing. Queue it up to be sent to the IMMULITE.
#if DEBUG
                    UniversaLIService.AppendToLog("Outgoing message, adding to queue...");
#endif
                    OutboundMessageQueue.Enqueue(message);
               }
               else if (headerFields[4] == receiver_id)
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
                              UniversaLIService.AppendToLog("Processing query.");
                              ProcessQuery(query);
                         }
                    }
                    else
                    {
                         int pID;
                         int oID;
#if DEBUG 
                         UniversaLIService.AppendToLog("Connecting to database.");
#endif
                         // This is where we have to connect to the database.
                         // TODO: Support internal database with SQLite
                         using (SqlConnection conn = new SqlConnection(UniversaLIService.YamlSettings?.ServiceConfig?.ConnectionString))
                         {
                              conn.Open();
#if DEBUG 
                              UniversaLIService.AppendToLog("Database connection open.");
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
                                   UniversaLIService.AppendToLog("Adding patient.");
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
                                        pID = int.Parse(command.Parameters["@ID"].Value.ToString() ?? "0");
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
                                        UniversaLIService.AppendToLog("Adding order.");
                                        using (SqlCommand command = new SqlCommand("spIMMOrder", conn))
                                        {
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
                                             oID = int.Parse(command.Parameters["@ID"].Value.ToString() ?? "0");
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
                                             UniversaLIService.AppendToLog("Adding result.");
                                             using (SqlCommand command = new SqlCommand("spIMMResult", conn))
                                             {
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
                                                  string dtStart = result.Elements["Date/Time Test Started"];
                                                  DateTime start = new DateTime(Convert.ToInt32(dtStart.Substring(0, 4)), Convert.ToInt32(dtStart.Substring(4, 2)), Convert.ToInt32(dtStart.Substring(6, 2)), Convert.ToInt32(dtStart.Substring(8, 2)), Convert.ToInt32(dtStart.Substring(10, 2)), Convert.ToInt32(dtStart.Substring(12, 2)));
                                                  _ = command.Parameters.AddWithValue("@TestStarted", start);
                                                  string dtEnd = result.Elements["Date/Time Test Completed"];
                                                  DateTime end = new DateTime(Convert.ToInt32(dtEnd.Substring(0, 4)), Convert.ToInt32(dtEnd.Substring(4, 2)), Convert.ToInt32(dtEnd.Substring(6, 2)), Convert.ToInt32(dtEnd.Substring(8, 2)), Convert.ToInt32(dtEnd.Substring(10, 2)), Convert.ToInt32(dtEnd.Substring(12, 2)));
                                                  _ = command.Parameters.AddWithValue("@TestCompleted", end);
                                                  char[] trimmings = { '\x03', '\x0D' };
                                                  _ = command.Parameters.AddWithValue("@Instrument_ID", result.Elements["Instrument ID"].Trim(trimmings));
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
                    UniversaLIService.AppendToLog($"Invalid message header! {message.MessageHeader}");
                    throw new ArgumentException($"Invalid message header: {message.MessageHeader}");
               }
          }

          private void ProcessQuery(Query query)
          {
               // Extract Sample Number from the query string.
               string SampleNumber = query.Elements["Starting Range"].Trim('^');
               string testID = query.Elements["Test ID"];
               // SendPatientOrders for the requested SampleNumber/testID.
               _ = SendPatientOrders(SampleNumber, testID);
          }

          public void ReceiptTimedOut(object? sender, EventArgs e)
          {
               CommState.RcvTimeout();
          }

          private int SendPatientOrders(string SampleNumber, string testID)
          {
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
               using (SqlConnection conn = new SqlConnection(UniversaLIService.YamlSettings?.ServiceConfig?.ConnectionString))
               {
                    conn.Open();
                    int orderCount;
                    using (SqlCommand sqlCommand = conn.CreateCommand())
                    { // Check to see how many orders are pending for the sample.
                         sqlCommand.CommandText = "SELECT COUNT(IMM_RequestOrders.ReqOrderID) AS OrderCount FROM IMM_RequestOrders WHERE (IMM_RequestOrders.Sample_Number LIKE @SampleNumber) AND (IMM_RequestOrders.PendingSending = 1)";
                         _ = sqlCommand.Parameters.AddWithValue("@SampleNumber", SampleNumber);
                         orderCount = (int)sqlCommand.ExecuteScalar();
                    }

                    if (orderCount == 0)
                    {    // No pending orders. 
                         if (isQuery)
                         {
                              // Reply with a "no information available from last query" message (terminator = "I")
                              UniversaLIService.AppendToLog($"No information available from last query (sample number: {SampleNumber})");
                              Message messageBody = new Message(this)
                              {
                                   Terminator = 'I'
                              };
                              ProcessMessage(messageBody);
                         }
                         // Exit function.
                         return orderCount;
                    }
                    Message responseMessage = new Message(this);
                    int patientCount;
                    using (SqlCommand sqlCommand = conn.CreateCommand())
                    { // Check to see how many patients are associated with the sample.
                         sqlCommand.CommandText = "SELECT COUNT(RequestPID) AS PatientCount FROM (SELECT RequestPID FROM vwPendingRequests WHERE (Sample_Number LIKE @SampleNumber) GROUP BY RequestPID) AS tmp";
                         _ = sqlCommand.Parameters.AddWithValue("@SampleNumber", SampleNumber);
                         patientCount = (int)sqlCommand.ExecuteScalar();
                    }

                    string sqlMainQuery = "SELECT * FROM vwPendingRequests WHERE Patient_Name LIKE @Patient_Name AND Test_ID LIKE @Test_ID AND Sample_Number LIKE @Sample_Number;";
                    string sqlPatientQuery = "SELECT Patient_ID, Patient_Name, BirthDate, Patient_Sex FROM vwPendingRequests WHERE (Sample_Number LIKE @Sample_Number) AND (Test_ID LIKE @Test_ID) GROUP BY Patient_ID, Patient_Name, BirthDate, Patient_Sex";


                    using (SqlCommand command = conn.CreateCommand())
                    {
                         command.CommandText = sqlPatientQuery;
                         command.Parameters.AddWithValue("@Sample_Number", SampleNumber);
                         if (testID == "ALL")
                         {
                              testID = "%";
                         }
                         command.Parameters.AddWithValue("@Test_ID", testID);
                         SqlDataReader patientReader = command.ExecuteReader();
                         if (!patientReader.HasRows)
                         {
                              return 0;
                         }
                         else
                         {
                              while (patientReader.Read())
                              {
                                   responseMessage.Patients.Add(new Patient($"|{responseMessage.Patients.Count + 1}|{patientReader["Patient_ID"]}|||{patientReader["Patient_Name"]}||{patientReader["BirthDate"]}|{patientReader["Patient_Sex"]}||||||||||||||||||||||||||"));
                              }
                         }

                         patientReader.Close();
                    }
                    foreach (var patient in responseMessage.Patients)
                    {
                         string? requestID = "";
                         using (SqlCommand orderCommand = conn.CreateCommand())
                         {
                              orderCommand.CommandText = sqlMainQuery;
                              orderCommand.Parameters.AddWithValue("@Patient_Name", patient.Elements["Patient Name"]);
                              orderCommand.Parameters.AddWithValue("@Sample_Number", SampleNumber);
                              if (testID == "ALL")
                              {
                                   testID = "%";
                              }
                              orderCommand.Parameters.AddWithValue("@Test_ID", testID);
                              SqlDataReader orderReader = orderCommand.ExecuteReader();
                              while (orderReader.Read())
                              {
                                   patient.Orders.Add(new Order($"|{patient.Orders.Count + 1}|{orderReader["Sample_Number"]}||{orderReader["Test_ID"]}|{orderReader["Priority"]}|{orderReader["Order_DateTime"]}|{orderReader["Collection_DateTime"]}|||||||||||{orderReader["RequestPID"]}||||||||||||", (int)orderReader["ReqOrderID"]));
                                   requestID = orderReader["RequestPID"].ToString();
                              }
                              orderReader.Close();
                         }
                         using (SqlCommand command = conn.CreateCommand())
                         {
                              command.CommandText = "UPDATE IMM_RequestOrders SET PendingSending = 0 WHERE RequestPID = @RequestID";
                              command.Parameters.AddWithValue("@RequestID", requestID);
                              command.ExecuteNonQuery();
                         }
                    }

                    if (isQuery)
                    {
                         responseMessage.Terminator = 'F';
                    }
                    else
                    {
                         responseMessage.Terminator = 'N';
                    }
                    ProcessMessage(responseMessage);
                    return orderCount;
               }
          }

          public void TransactionTimedOut(object? sender, EventArgs e)
          {
               CommState.TransTimeout();
          }

          public void WorklistTimedEvent(object? source, System.Timers.ElapsedEventArgs e)
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
     }
}