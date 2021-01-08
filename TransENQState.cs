using static IMMULIS.IMMULIService;

namespace IMMULIS
{
     class TransENQState : ILISState
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
               // Send next frame.
               CurrentMessage = OutboundMessageQueue.Dequeue();
               CurrentMessage.PrepareToSend();
               ComPort.Send(CurrentMessage.FrameList[CurrentFrameCounter]);
               CurrentFrameCounter++;
               // Reset the NAK count to 0.
               numNAK = 0;
               // Reset the transaction timer to 15 seconds.
               transTimer.Reset(15);
          }

          public void RcvData(string InputString)
          {
               // Ignore
          }

          public void RcvENQ()
          {
               // Set the contention timer to 20 seconds and return to idle state.
               ContentTimer.Reset(20);
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
               // If the instrument responds to our ENQ with NAK, it's busy. Back to Idle for 10 seconds.
               BusyTimer.Reset(10);
          }

          void ILISState.HaveData()
          {
               throw new System.NotImplementedException();
          }
     }
}
