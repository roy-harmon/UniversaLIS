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
               AppendToLog("CurrentMessage.FrameList.Count: " + CurrentMessage.FrameList.Count);
               AppendToLog("CurrentFrameCounter: " + CurrentFrameCounter);
               // If all frames have been sent, end the transmission and reset the frame counter.
               if (CurrentMessage.FrameList.Count == CurrentFrameCounter)
               {
                    ComPort.Send(Constants.EOT);
                    CurrentFrameCounter = 0;
                    transTimer.Reset(-1);
                    CurrentMessage = new MessageBody();
               }
               else
               {
                    // Send next frame.
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
               // This shouldn't come up, so ignore it for now. Maybe we'll add some logging here when we have time.
          }

          public void RcvENQ()
          {
               // This really shouldn't happen. Let's ignore it and see if it goes away.
          }

          public void RcvEOT()
          {
               /* This is a Receiver Interrupt request. 
               *  Ideally, this would cause the host to stop transmitting, enter the idle state,
               *  and not try to send again for at least 15 seconds.
               *  
               *  Or, we could choose not to honor the interrupt request, 
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
               throw new NotImplementedException();
          }
     }
}
