using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.IO.Ports;
using System.Linq;
using System.Text;
using static IMMULIS.ServiceMain;
// TODO: Escape special characters in message fields, remove any delimiter characters within field contents.
namespace IMMULIS
{
     public class CommFacilitator
     {
          private bool bInterFrame = false;

          public CountdownTimer BusyTimer = new CountdownTimer(-1);

          public LISCommState commState = new LISCommState();

          public CommPort ComPort;

          public CountdownTimer ContentTimer = new CountdownTimer(-1);

          public int CurrentFrameCounter;

          public Message CurrentMessage;

          private string intermediateFrame;

          public int numNAK = 0;

          public Queue<Message> OutboundMessageQueue = new Queue<Message>();

          public CountdownTimer rcvTimer;

          public CountdownTimer transTimer;

          private readonly System.Timers.Timer idleTimer = new System.Timers.Timer(Properties.Settings.Default.AutoSendInterval);

          internal string receiver_id;

          // Update this string when setting up external database connection functions.
          string connString = @"\internal.db";

          public CommFacilitator(string portName, int baudRate, string parity, int databits, string stopbits, string handshake, string receiverID)
          {
               commState.comm = this;
               rcvTimer = new CountdownTimer(-1, ReceiptTimedOut);
               transTimer = new CountdownTimer(-1, TransactionTimedOut);
               try
               {
                    // Set the serial port properties and try to open it.
                    receiver_id = receiverID;
                    ComPort = new CommPort(portName, baudRate, (Parity)Enum.Parse(typeof(Parity), parity, true), databits, (StopBits)Enum.Parse(typeof(StopBits), stopbits, true)) //Properties.Settings.Default.SerialPorts, Properties.Settings.Default.SerialPortBaudRate, Properties.Settings.Default.SerialPortParity, Properties.Settings.Default.SerialPortDataBits, Properties.Settings.Default.SerialPortStopBits)
                    {
                         Handshake = (Handshake)Enum.Parse(typeof(Handshake),handshake, true), 
                         ReadTimeout = 20,
                         WriteTimeout = 20
                    };
                    CurrentMessage = new Message(this);
                    ComPort.Encoding = Encoding.UTF8;
                    // Set the handler for the DataReceived event.
                    ComPort.DataReceived += ComPortDataReceived;
                    ComPort.Open();
                    AppendToLog($"Port opened: {portName}");
                    idleTimer.AutoReset = true;
                    idleTimer.Elapsed += new System.Timers.ElapsedEventHandler(IdleTime);
                    if (Properties.Settings.Default.AutoSendOrders == true) { idleTimer.Elapsed += WorklistTimedEvent; }
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    HandleEx(ex);
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

                         using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
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
                    HandleEx(ex);
                    throw;
               }
          }
          void ComPortDataReceived(object sender, SerialDataReceivedEventArgs e ) //DataReceivedArgs e)
          {
               /* When new data is received, 
                * parse the message line-by-line.
                */
               CommPort sp = (CommPort)sender;
               string buffer = ""; // = System.Text.Encoding.Default.GetString(e.Data);
               bool timedOut = false;
               try
               {
                    idleTimer.Stop();
#if DEBUG
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew(); // This stopwatch is an attempt at performance optimization. 
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
                    ServiceMain.AppendToLog($"Elapsed port read time: {stopwatch.ElapsedMilliseconds}");
#endif
                    ServiceMain.AppendToLog($"In: \t{buffer}");
                    commState.RcvInput(buffer);
                    idleTimer.Start();
               }
               catch (Exception ex)
               {
                    ServiceMain.HandleEx(ex);
                    throw;
               }
          }

          private void IdleTime(object o, System.Timers.ElapsedEventArgs elapsedEvent)
          {
               idleTimer.Stop();
               commState.IdleCheck();
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
                    ServiceMain.AppendToLog($"Invalid message: {messageLine}");
                    return;
               }
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
                         CurrentMessage.Patients[CurrentMessage.Patients.Count() - 1].Orders.Add(new Order(messageLine));
                         break;

                    case "R": // Result record.
                         CurrentMessage.Patients[CurrentMessage.Patients.Count - 1].Orders[CurrentMessage.Patients[CurrentMessage.Patients.Count - 1].Orders.Count - 1].Results.Add(new Result(messageLine));
                         break;

                    case "L": // Terminator record.
                         if (messageLine.Substring(5, 1) == Constants.CR)
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

          public void ProcessMessage(Message message)
          {
               /* The message is complete. Deal with it. */
#if DEBUG
               ServiceMain.AppendToLog("Processing message.");
#endif
               if (message == null)
               {
                    ServiceMain.AppendToLog("MessageBody is null!");
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
                    ServiceMain.AppendToLog("Outgoing message, adding to queue...");
#endif
                    OutboundMessageQueue.Enqueue(message);

                    return;
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
                              ServiceMain.AppendToLog("Processing query.");
                              ProcessQuery(query);
                         }
                    }
                    else
                    {
                         int pID;
                         int oID;
#if DEBUG 
                         ServiceMain.AppendToLog("Connecting to database.");
#endif
                         // This is where we have to connect to the database.
                         // TODO: Switch to SQLite
                         using (SqlConnection conn = new SqlConnection(Properties.Settings.Default.ConnectionString))
                         {
                              conn.Open();
#if DEBUG 
                              ServiceMain.AppendToLog("Database connection open.");
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
                                   ServiceMain.AppendToLog("Adding patient.");
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
                                        ServiceMain.AppendToLog("Adding order.");
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
                                             ServiceMain.AppendToLog("Adding result.");
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
                    ServiceMain.AppendToLog($"Invalid message received! {message.MessageHeader}");
                    throw new ArgumentException();
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

          public void ReceiptTimedOut(object sender, EventArgs e)
          {
               commState.RcvTimeout();
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
                              ServiceMain.AppendToLog($"No information available from last query (sample number: {SampleNumber})");
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
                         if (patientReader.HasRows == false)
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
                         string requestID = "";
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

                    if (isQuery == true)
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

          public void TransactionTimedOut(object sender, EventArgs e)
          {
               commState.TransTimeout();
          }

          public void WorklistTimedEvent(object source, System.Timers.ElapsedEventArgs e)
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