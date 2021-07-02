using System;
using static IMMULIS.IMMULIService;

namespace IMMULIS
{
     class TransWaitState : ILISState
     {
          public void RcvInput(string InputString)
          {
               switch (InputString)
               {
                    case Constants.ACK:
                         RcvACK();
                         break;
                    case Constants.NAK:
                         RcvNAK();
                         break;
                    case Constants.ENQ:
                         RcvENQ();
                         break;
                    case Constants.EOT:
                         RcvEOT();
                         break;
                    default:
                         RcvData(InputString);
                         break;
               }
          }
          public void RcvACK()
          {
#if DEBUG
               AppendToLog("CurrentMessage.FrameList.Count: " + CurrentMessage.FrameList.Count);
               AppendToLog("CurrentFrameCounter: " + CurrentFrameCounter);
#endif
               // If all frames have been sent, end the transmission.
               if (CurrentMessage.FrameList.Count == CurrentFrameCounter)
               {
                    ComPort.Send(Constants.EOT);
                    CurrentMessage = new MessageBody();
               }
               else
               {
                    // Otherwise, send next frame.
                    ComPort.Send(CurrentMessage.FrameList[CurrentFrameCounter]);
                    CurrentFrameCounter++;
                    // Reset the NAK count to 0.
                    numNAK = 0;
                    // Reset the transaction timer to 15 seconds.
                    transTimer.Reset(15);
               }
          }

          public void RcvData(string InputString)
          {
               // Data frames should always be preceded by other signals, so for now, just log it.
               // Signal the instrument to interrupt the transmission with an EOT.
               AppendToLog("Data received in TransWait state: " + InputString);
               ComPort.Send(Constants.EOT);
          }

          public void RcvENQ()
          {
               // This really shouldn't happen.
               // The instrument is supposed to send an EOT first to get us to stop transmitting.
               // Nevertheless, log the occurrence for troubleshooting purposes.
               AppendToLog("ENQ received in TransWait state.");
          }

          public void RcvEOT()
          {
               /* This is a Receiver Interrupt request. 
               *  Ideally, this would cause the host to stop transmitting, enter the idle state,
               *  and not try to send again for at least 15 seconds.
               *  TODO: Honor Receiver Interrupt requests.
               *  Or, we could choose to ignore the interrupt request, 
               *  in which case we could treat this as a positive acknowledgement and keep going.
               *  We'll start with that for now.
               */
               RcvACK();
          }

          public void RcvNAK()
          {
               // Send old frame.
               ComPort.Send(CurrentMessage.FrameList[CurrentFrameCounter - 1]);
               // Increment NAK count.
               numNAK++;
               if (numNAK == 6)
               {
                    // Too many NAKs. Something's wrong. Send an EOT and go back to Idle.
                    // Maybe stick the message back in the queue to try again later?
                    ComPort.Send(Constants.EOT);
                    OutboundMessageQueue.Enqueue(CurrentMessage);
                    CurrentMessage = new MessageBody();
               }
          }

          void ILISState.HaveData()
          {
               // It doesn't matter if we have data to send. We're already sending something.
               AppendToLog("HaveData called in TransWait state.");
          }
     }
}
