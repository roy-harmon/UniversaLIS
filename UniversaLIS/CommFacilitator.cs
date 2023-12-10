using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using UniversaLIS.Models;
using UniversaLIS.States;

// TODO: Escape special characters in message fields, remove any delimiter characters within field contents.
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

          public Queue<Message> OutboundInstrumentMessageQueue { get; set; } = new Queue<Message>();

          public CountdownTimer RcvTimer { get; set; }

          public CountdownTimer TransTimer { get; set; }

          private readonly System.Timers.Timer idleTimer = new();

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
               CurrentMessage = NewMessage();
               try
               {
                    // Set the handler for the DataReceived event.
                    ComPort.PortDataReceived += CommPortDataReceived!;
                    ComPort.Open();
                    UniversaLIService.AppendToLog($"Port opened: {serialSettings.Portname}");
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
                    UniversaLIService.AppendToLog($"Error opening port: {serialSettings.Portname} Not Found!");
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
                    CurrentMessage = NewMessage();
                    ComPort.Open();
                    UniversaLIService.AppendToLog($"Socket opened: {tcpSettings.Socket}");
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
          
          public Message NewMessage()
          {
               return new Message(frameSize,
                                  password,
                                  service.GetYamlSettings()?.ServiceConfig?.LisId,
                                  service.GetYamlSettings()?.ServiceConfig?.Address,
                                  service.GetYamlSettings()?.ServiceConfig?.Phone,
                                  GetPortDetails(),
                                  receiver_id);
          }

          public Message NewMessage(char terminator)
          {
               Message message = NewMessage();
               message.Terminator = terminator;
               return message;
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
                         // Send each message's patient records to the REST-LIS API to reset their Pending status.
                         service.SendRestLisRequest(HttpMethod.Post, "/requests/pending", message.Patients);
                    }
                    ComPort.Close();
                    UniversaLIService.AppendToLog($"Port closed: {ComPort.PortName}");
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
               StringBuilder buffer = new();
               try
               {
                    idleTimer.Stop();
                    /* There are a few messages that won't end in a NewLine,
                     * so we have to read one character at a time until we run out of them.
                     */
                    // Read one char at a time until the ReadChar times out.
                    buffer.Append(port.ReadChars());
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
                         intermediateFrame += messageLine[..position];
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
               if (message == null)
               {
                    UniversaLIService.AppendToLog("MessageBody is null!");
                    return;
               }
               // First, check to see whether the LIS is sending or receiving the message.
               if (message.Direction == Message.MessageDirection.Outbound)
               {
                    // Message is outgoing. Queue it up to be sent to the instrument.
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
                    service.SendRestLisRequest(HttpMethod.Post, "/reports", message.Patients.Cast<Patient>());
               }
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
               string endpoint = "/requests/samples";
               // TODO: Find a way to handle delimited lists of test codes.
               if (testID == "ALL")
               {
                    testID = "%";
               }
               // If no SampleNumber is provided, get all pending orders.
               if (SampleNumber.Length == 0)
               {
                    SampleNumber = "%";
               }
               else
               {
                    endpoint = $"{endpoint}/{SampleNumber}";
               }
               if (SampleNumber == "%")
               {
                    isQuery = false;
               }
               // Query the REST-LIS server for [P]atient and [O]rder records for the sample.
               HttpResponseMessage response = service.SendRestLisRequest(HttpMethod.Get, endpoint, "");
               Stream responseStream = response.Content.ReadAsStream();
               List<PatientRequest> patientRequests = JsonSerializer.Deserialize<List<PatientRequest>>(responseStream) ?? new();
               if (isQuery && patientRequests.Count == 0) {
                    // Reply with a "no information available from last query" message (terminator = "I")
                    UniversaLIService.AppendToLog($"No information available from last query (sample number: {SampleNumber})");
                    Message messageBody = NewMessage('I');
                    ProcessMessage(messageBody);
               }
               Message responseMessage = NewMessage();
               responseMessage.Patients.AddRange(patientRequests);
               if (isQuery)
               {
                    responseMessage.Terminator = 'F';
               }
               else
               {
                    responseMessage.Terminator = 'N';
               }
               ProcessMessage(responseMessage);

               return patientRequests.Count;
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