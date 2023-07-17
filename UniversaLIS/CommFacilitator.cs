using Microsoft.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using UniversaLIS.Models;
using UniversaLIS.States;
using static UniversaLIS.UniversaLIService;
// TODO: Escape special characters in message fields, remove any delimiter characters within field contents.
// TODO: Consider field mappings when constructing SQL commands.
namespace UniversaLIS
{

    public class CommFacilitator
     {
          private const string LAST_INSERTED = "select last_insert_rowid();";
          private bool bInterFrame = false;
          private readonly IPortAdapter ComPort;
          private readonly IPortSettings portSettings;
          internal UniversaLIService service;
          public CountdownTimer ContentTimer { get; set; } = new CountdownTimer(-1);

          public int CurrentFrameCounter { get; set; }

          public Message CurrentMessage { get; set; }

          private string? intermediateFrame;

          public int NumNAK { get; set; } = 0;

          public Queue<Message> OutboundInstrumentMessageQueue { get; set; } = new Queue<Message>();

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
          public const string INTERNAL_CONNECTION_STRING = "Data Source=../UniversaLIS/internal.db";

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
               // Set the serial port properties and try to open it.
               receiver_id = serialSettings.ReceiverId;
               ComPort = new CommPort(serialSettings, this);
               CommState = new LisCommState(this);
               RcvTimer = new CountdownTimer(-1, ReceiptTimedOut);
               TransTimer = new CountdownTimer(-1, TransactionTimedOut);
               CurrentMessage = new Message(this);
               try
               {
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
               catch (FileNotFoundException ex)
               {
                    AppendToLog($"Error opening port: {serialSettings.Portname} Not Found!");
                    service.HandleEx(ex);
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
                    /* Update the request table so that any unsent messages will be sent next time the service runs.
                     */
                    while (OutboundInstrumentMessageQueue.Count > 0)
                    {
                         Message message = OutboundInstrumentMessageQueue.Dequeue();

                         using (DbConnection conn = new SqliteConnection(INTERNAL_CONNECTION_STRING))
                         {
                              foreach (Patient patientItem in message.Patients)
                              {
                                   foreach (Order orderItem in patientItem.Orders)
                                   {
                                        using (DbCommand command = conn.CreateCommand())
                                        {
                                             command.CommandText = "UPDATE OrderRequest SET PendingSending = 1 WHERE OrderID = @OrderID";
                                             DbParameter parameter = command.CreateParameter();
                                             parameter.ParameterName = "@OrderID";
                                             parameter.Value = orderItem.OrderID;
                                             command.Parameters.Add(parameter);
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
               if (position > 0)
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
                    return; // Don't process yet; this is an intermediate frame with more to come.
               }
               else
               {
                    if (bInterFrame) // For a continuation of an intermediate frame, trim the frame number and concatenate with previous frame.
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
                         CurrentMessage.Patients[^1].Orders.Add(new Order(messageLine, (Patient)CurrentMessage.Patients[^1]));
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

                    // TODO: Implement Comment and Scientific records.
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
               // First, check to see whether the LIS is sending or receiving the message.
               if (message.Direction == Message.MessageDirection.Outbound)
               {
                    // Message is outgoing. Queue it up to be sent to the instrument.
#if DEBUG
                    UniversaLIService.AppendToLog("Outgoing message, adding to queue...");
#endif
                    OutboundInstrumentMessageQueue.Enqueue(message);
               }
               else if (message.Direction == Message.MessageDirection.Inbound)
               {
                    ReceiveIncomingMessage(message);
               }
               else
               {
                    // This message is invalid!
                    UniversaLIService.AppendToLog($"Invalid message direction! {message.MessageHeader}");
                    throw new ArgumentException($"Invalid message direction. {message.MessageHeader}");
               }
          }

          private void ReceiveIncomingMessage(Message message)
          {
               // Message is incoming. Add the contents to the appropriate database tables.
               /* For our purposes, there are two primary incoming message types
                * (from the instrument to the LIS):
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
                    long pID;
                    long oID;
#if DEBUG
                    UniversaLIService.AppendToLog("Connecting to database.");
#endif
                    // This is where we have to connect to the database.
                    using (DbConnection conn = new SqliteConnection(INTERNAL_CONNECTION_STRING))
                    {
                         conn.Open();
#if DEBUG
                         UniversaLIService.AppendToLog("Database connection open.");
#endif
                         const string NEW_PATIENT = "INSERT INTO PatientRecord (PracticePatientID, LabPatientID, PatientID3, PatientName, MMName, DOB, Sex, Race, Address, Reserved, TelNo," +
                              " AttendingPhysicianID, Special1, Special2, Height, Weight, Diagnosis, ActiveMeds, Diet, PF1, PF2, AdmDates, AdmStatus, Location, AltCodeNature, AltCode, Religion," +
                              " MaritalStatus, IsolationStatus, Language, HospService, HospInstitution, DosageCategory) VALUES (@PracticePatientID, @LabPatientID, @PatientID3, @PatientName," +
                              " @MMName, @DOB, @Sex, @Race, @Address, @Reserved, @TelNo, @AttendingPhysicianID, @Special1, @Special2, @Height, @Weight, @Diagnosis, @ActiveMeds, @Diet, @PF1, @PF2," +
                              " @AdmDates, @AdmStatus, @Location, @AltCodeNature, @AltCode, @Religion, @MaritalStatus, @IsolationStatus, @Language, @HospService, @HospInstitution, @DosageCategory);";
                         const string NEW_ORDER = "INSERT INTO OrderRecord (PatientID, SpecimenID, InstrSpecID, UniversalTestID, Priority, OrderDate, CollectionDate, CollectionEndTime," +
                              " CollectionVolume, CollectorID, ActionCode, DangerCode, RelevantClinicInfo, SpecimenRecvd, SpecimenDescriptor, OrderingPhysician, PhysicianTelNo, UF1, UF2, LF1, LF2," +
                              " LastReported, BillRef, InstrSectionID, ReportType, Reserved, SpecCollectLocation, NosInfFlag, SpecService, SpecInstitution) VALUES (@Patient_ID, @SpecimenID," +
                              " @InstrSpecID, @UniversalTestID, @Priority, @OrderDate, @CollectionDate, @CollectionEndTime, @CollectionVolume, @CollectorID, @ActionCode, @DangerCode, @RelevantClinicInfo," +
                              " @SpecimenRecvd, @SpecimenDescriptor, @OrderingPhysician, @PhysicianTelNo, @UF1, @UF2, @LF1, @LF2, @LastReported, @BillRef, @InstrSectionID, @ReportType, @Reserved," +
                              " @SpecCollectLocation, @NosInfFlag, @SpecService, @SpecInstitution);";
                         const string NEW_RESULT = "INSERT INTO ResultRecord (OrderID, UniversalTestID, ResultValue, Unit, RefRange, Abnormal, AbNature, ResStatus, NormsChanged, OperatorID, TestStart," +
                              " TestEnd, InstrumentID) VALUES (@Order_ID, @UniversalTestID, @ResultValue, @Unit, @RefRange, @Abnormal, @AbNature, @ResStatus, @NormsChanged, @OperatorID, @TestStart, @TestEnd, @InstrumentID);";

                         foreach (var patient in message.Patients)
                         {
                              /* * Patient fields: * *
                               *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                               *  These fields may or may not actually be functional (see the instrument's specification for details)
                               *  so initial development efforts will focus primarily on known supported fields.
                               * 0 Record Type
                               * 1 Sequence# Definition
                               * 2 Practice Assigned PatientID3
                               * 3 LabPatientID  =/=
                               * 4 PatientID3  =/=
                               * 5 PatientName
                               * 6 MMName  =/=
                               * 7 DOB *
                               * 8 Patient's Sex
                               * 9 Race-Ethnic Origin  =/=
                               *10 Patient's Address  =/=
                               *11 Reserved  =/=
                               *12 Patient's Phone#  =/=
                               *13 AttendingPhysicianID  =/=
                               *14 Special1  =/=
                               *15 Special2  =/=
                               *16 Height  =/=
                               *17 Weight  =/=
                               *18 Diagnosis  =/=
                               *19 ActiveMeds  =/=
                               *20 Diet  =/=
                               *21 PF1  =/=
                               *22 PF2  =/=
                               *23 AdmDates  =/= *
                               *24 AdmStatus  =/=
                               *25 Location  =/=
                               *26 AltCodeNature  =/=
                               *27 AltCode  =/=
                               *28 Religion  =/=
                               *29 MaritalStatus  =/=
                               *30 IsolationStatus  =/=
                               *31 Language  =/=
                               *32 HospService  =/=
                               *33 HospInstitution  =/=
                               *34 DosageCategory  =/=
                               */
                              UniversaLIService.AppendToLog("Adding patient.");
                              // Transmit a patient record to the PatientRecord table,
                              // grab the row ID, and use it to add order records to the OrderRecord table.
                              using (DbCommand command = conn.CreateCommand())
                              {
                                   command.CommandText = NEW_PATIENT;
                                   foreach(DictionaryEntry element in patient.Elements)
                                   {
                                        switch (element.Key)
                                        {
                                             case "FrameNumber":
                                             case "Sequence#":
                                                  break;
                                             default:
                                                  AddWithValue(command, $"@{element.Key}", $"{element.Value}" == "" ? DBNull.Value : $"{element.Value}");
                                                  break;
                                        }
                                   }
                                   command.ExecuteNonQuery();
                              }
                              using (DbCommand command = conn.CreateCommand())
                              {
                                   command.CommandText = LAST_INSERTED;
                                   pID = (long)(command.ExecuteScalar() ?? 0);
                              }
                              foreach (Order order in patient.Orders)
                              {
                                   /*
                                   *  * Order Fields: * *
                                   *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                                   *  These fields may or may not actually be functional.
                                   *	0	Record Type (O)
                                   *	1	Sequence#
                                   *	2	SpecimenID
                                   *	3	InstrSpecID
                                   *	4	UniversalTestID
                                   *	5	Priority
                                   *	6	OrderDate*
                                   *	7	CollectionDate*
                                   *	8	CollectionEndTime*
                                   *	9	CollectionVolume
                                   *	10	CollectorID
                                   *	11	ActionCode
                                   *	12	DangerCode
                                   *	13	RelevantClinicInfo
                                   *	14	SpecimenRecvd*
                                   *	15	SpecimenDescriptor,Specimen Type,Specimen Source
                                   *	16	OrderingPhysician
                                   *	17	PhysicianTelNo
                                   *	18	UF1
                                   *	19	UF2
                                   *	20	LF1
                                   *	21	LF2
                                   *	22	LastReported*
                                   *	23	BillRef
                                   *	24	InstrSectionID
                                   *	25	ReportType
                                   *	26	Reserved
                                   *	27	SpecCollectLocation
                                   *	28	NosInfFlag
                                   *	29	SpecService
                                   *	30	SpecInstitution
                                    */
                                   UniversaLIService.AppendToLog("Adding order.");
                                   using (DbCommand command = conn.CreateCommand())
                                   {
                                        command.CommandText = NEW_ORDER;
                                        AddWithValue(command, "@Patient_ID", pID);
                                        IDictionaryEnumerator enumerator = order.Elements.GetEnumerator();
                                        while (enumerator.MoveNext())
                                        {
                                             switch (enumerator.Key)
                                             {
                                                  case "FrameNumber":
                                                  case "Sequence#":
                                                       break;
                                                  default:
                                                       AddWithValue(command, $"@{enumerator.Key}", $"{enumerator.Value}" == "" ? DBNull.Value : $"{enumerator.Value}");
                                                       break;
                                             }
                                        }
                                        command.ExecuteNonQuery();
                                   }
                                   using (DbCommand command = conn.CreateCommand())
                                   {
                                        command.CommandText = LAST_INSERTED;
                                        oID = (long)(command.ExecuteScalar() ?? 0);
                                   }
                                   // Use the row ID from each of those order records to add
                                   // result records to the IMM_Results table for each Patient.Order.Result in the message.
                                   foreach (var result in order.Results)
                                   {
                                        /* * Result Fields: * *
                                        *  Note: Fields marked with "=/=" are listed in the Siemens interface specification as not officially supported.
                                        *  These fields may or may not actually be functional.
                                        *	0	Record Type (R)
                                        *	1	Sequence#
                                        *	2	UniversalTestID
                                        *	3	Result
                                        *	4	Unit
                                        *	5	ReferenceRanges
                                        *	6	Abnormal
                                        *	7	AbNature
                                        *	8	ResStatus
                                        *	9	NormsChanged
                                        *	10	OperatorID
                                        *	11	Date\Time Test Started
                                        *	12	Date\Time Test Completed
                                        *	13	InstrumentID
                                         */
                                        UniversaLIService.AppendToLog("Adding result.");
                                        using (DbCommand command = conn.CreateCommand())
                                        {
                                             command.CommandText = NEW_RESULT;
                                             AddWithValue(command, "@Order_ID", oID);
                                             IDictionaryEnumerator enumerator = result.Elements.GetEnumerator();
                                             while (enumerator.MoveNext())
                                             {
                                                  switch (enumerator.Key)
                                                  {
                                                       case "InstrumentID":
                                                            char[] trimmings = { '\x03', '\x0D' };
                                                            AddWithValue(command, $"@{enumerator.Key}", $"{enumerator.Value}" == "" ? DBNull.Value : $"{enumerator.Value}".Trim(trimmings));
                                                            break;
                                                       case "FrameNumber":
                                                       case "Sequence#":
                                                            break;
                                                       default:
                                                            AddWithValue(command, $"@{enumerator.Key}", $"{enumerator.Value}" == "" ? DBNull.Value : $"{enumerator.Value}");
                                                            break;
                                                  }
                                             }
                                             command.ExecuteNonQuery();
                                        }
                                   }
                              }
                         }
                    }
               }
          }

          private static void AddWithValue(DbCommand command, string parameterName, object? value)
          {
               DbParameter parameter = command.CreateParameter();
               parameter.ParameterName = parameterName;
               parameter.Value = value;
               command.Parameters.Add(parameter);
          }

          private void ProcessQuery(Query query)
          {
               // Extract Sample Number from the query string.
               string SampleNumber = query.Elements["Starting Range"].Trim('^');
               string testID = query.Elements["Test ID"];
               // SendPatientOrders for the requested SampleNumber/testID.
               SendPatientOrders(SampleNumber, testID);
          }

          public void ReceiptTimedOut(object? sender, EventArgs e)
          {
               CommState.RcvTimeout();
          }

          private long SendPatientOrders(string SampleNumber, string testID)
          {
               bool isQuery = true;
               // TODO: Find a way to handle delimited lists of test codes.
               if (testID == "ALL")
               {
                    testID = "%";
               }
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
               using (DbConnection conn = new SqliteConnection(INTERNAL_CONNECTION_STRING))
               {
                    conn.Open();
                    long orderCount;
                    using (DbCommand sqlCommand = conn.CreateCommand())
                    { // Check to see how many orders are pending for the sample.
                         sqlCommand.CommandText = "SELECT COUNT(OrderRequest.OrderID) AS OrderCount FROM OrderRequest WHERE (SpecimenID LIKE @SampleNumber) AND (UniversalTestID LIKE @TestID) AND (PendingSending = 1);";
                         AddWithValue(sqlCommand, "@SampleNumber", SampleNumber);
                         AddWithValue(sqlCommand, "@TestID", testID);
                         orderCount = (long)(sqlCommand.ExecuteScalar() ?? 0L);
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
                    string selectPatientsQuery = "SELECT DISTINCT PatientRequest.* FROM PatientRequest JOIN OrderRequest" +
                         " WHERE (SpecimenID LIKE @Sample_Number) AND (UniversalTestID LIKE @Test_ID) AND PendingSending = 1;";
                    string selectOrdersQuery = "SELECT * FROM OrderRequest WHERE PatientID LIKE @Patient_ID" +
                         " AND UniversalTestID LIKE @Test_ID AND SpecimenID LIKE @Sample_Number AND PendingSending = 1;";


                    using (DbCommand command = conn.CreateCommand())
                    {
                         command.CommandText = selectPatientsQuery;
                         AddWithValue(command, "@Sample_Number", SampleNumber);
                         AddWithValue(command, "@Test_ID", testID);
                         DbDataReader patientReader = command.ExecuteReader();
                         if (!patientReader.HasRows)
                         {
                              return 0;
                         }
                         else
                         {
                              while (patientReader.Read())
                              {
                                   PatientRequest patient = new PatientRequest();
                                   patient.Elements["Sequence#"] = $"{responseMessage.Patients.Count + 1}"; 
                                   for (int i = 0; i < patientReader.FieldCount; i++)
                                   {
                                        string fieldName = patientReader.GetName(i);
                                        switch (fieldName)
                                        {
                                             case "PatientID":
                                                  patient.PatientID = patientReader.GetInt32(i);
                                                  break;
                                             default:
                                                  patient.Elements[fieldName] = $"{patientReader[fieldName]}";
                                                  break;
                                        }
                                   }
                                   responseMessage.Patients.Add(patient);
                              }
                         }

                         patientReader.Close();
                    }
                    foreach (var patient in responseMessage.Patients)
                    {
                         using (DbCommand orderCommand = conn.CreateCommand())
                         {
                              orderCommand.CommandText = selectOrdersQuery;
                              AddWithValue(orderCommand, "@Patient_ID", patient.PatientID);
                              AddWithValue(orderCommand, "@Sample_Number", SampleNumber);
                              AddWithValue(orderCommand, "@Test_ID", testID);
                              DbDataReader orderReader = orderCommand.ExecuteReader();
                              while (orderReader.Read())
                              {
                                   OrderRequest order = new OrderRequest((PatientRequest)patient);
                                   order.Elements["Sequence#"] = $"{patient.Orders.Count + 1}";
                                   for (int i = 0; i < orderReader.FieldCount; i++)
                                   {
                                        string fieldName = orderReader.GetName(i);
                                        switch (fieldName)
                                        {
                                             case "OrderID":
                                                  order.OrderID = Convert.ToInt32(orderReader[fieldName]);
                                                  break;
                                             case "PatientID":
                                             case "PendingSending":
                                                  break;
                                             default:
                                                  order.Elements[fieldName] = $"{orderReader[fieldName]}";
                                                  break;
                                        }
                                   }
                                   patient.Orders.Add(order);
                              }
                              orderReader.Close();
                         }
                         foreach (var order in patient.Orders)
                         {
                              using (DbCommand command = conn.CreateCommand())
                              {
                                   command.CommandText = "UPDATE OrderRequest SET PendingSending = 0 WHERE OrderID = @RequestID";
                                   AddWithValue(command, "@RequestID", order.OrderID);
                                   command.ExecuteNonQuery();
                             }
                              
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
                * and their respective patient records to send them all to the instrument.
                */
               // Stop the timer while any pending requests are sent.
               idleTimer.Stop();
               // Until the program is more stable, only add to the queue when it's empty.
               if (OutboundInstrumentMessageQueue.Count == 0)
               {
                    SendPatientOrders("%", "ALL");
               }
               // Now that all pending requests have been sent, restart the timer.
               idleTimer.Start();
          }
     }
}