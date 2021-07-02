using static IMMULIS.IMMULIService;

namespace IMMULIS
{
     public class LISCommState : ILISState
     {
          public ILISState CommState { get; set; }
          public LISCommState()
          {
               CommState = new IdleState();
          }
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
               CommState.RcvACK();
               if (CommState is TransENQState)
               {
                    // Transition to TransWaitState.
                    ChangeToTransWaitState();
               }
               if (CommState is IdleState) // In case the ACK comes back before we've transitioned to TransENQState. 
               {
                    // DEBUG: Better keep track of how often this happens, for testing purposes.
                    AppendToLog("RcvACK in IdleState!");
                    // Transition to TransENQState?
                    ChangeToTransENQState();
                    CommState.RcvACK();
                    // Transition to TransWaitState.
                    ChangeToTransWaitState();
               }
          }

          public void RcvData(string InputString)
          {
               CommState.RcvData(InputString);
          }

          public void RcvENQ()
          { 
               CommState.RcvENQ();
               if (CommState is IdleState)
               {
                    // Transition to TransWaitState
                    ChangeToRcvWaitState();
               }
               if (CommState is TransENQState)
               {
                    // Transition to IdleState
                    ChangeToIdleState();
               }
          }

          public void RcvEOT()
          {
               CommState.RcvEOT();
               if (CommState is RcvWaitState)
               {
                    // Return to Idle.
                    ChangeToIdleState();
                    if (OutboundMessageQueue.Count > 0)
                    {
                         // Don't make the operator wait for the timer tick.
                         IdleCheck();
                    }
               }
          }

          public void RcvNAK()
          {
               CommState.RcvNAK();
               if (CommState is TransWaitState && numNAK == 6)
               { // recieved too many NAKs in TransWaitSate which means the message was not going through, so give up and go back to IdleState
                    // Transition to IdleState
                    ChangeToIdleState();
               }
               if (CommState is TransENQState)
               { // machine is busy
                    // Transition to IdleState
                    ChangeToIdleState();
               }
          }

          public void HaveData()
          {
               if (CommState is IdleState)
               { // Execute the IdleState's HaveData to start the transfer
                    CommState.HaveData();
                    // Change state
                    ChangeToTransENQState();
#if DEBUG
                    AppendToLog("HaveData complete.");
#endif
               }
          }

          public void ChangeToIdleState()
          {
               CommState = new IdleState();
               transTimer.Reset(-1);
#if DEBUG
               AppendToLog("CommState changed to IdleState!");
#endif
          }
          public void ChangeToTransENQState()
          {
               CommState = new TransENQState();
#if DEBUG
               AppendToLog("CommState changed to TransENQState!");
#endif
          }
          public void ChangeToTransWaitState()
          {
               CommState = new TransWaitState();
#if DEBUG
               AppendToLog("CommState changed to TransWaitState!");
#endif
          }
          public void ChangeToRcvWaitState()
          {
               CommState = new RcvWaitState();
#if DEBUG
               AppendToLog("CommState changed to RcvWaitState!");
#endif
          }

          public void TransTimeout()
          {
               if (CommState is TransENQState || CommState is TransWaitState)
               {
                    // Send EOT and return to idle state.
                    ComPort.Send(Constants.EOT);
                    if (CommState is TransWaitState)
                    {
                         if (CurrentMessage.FrameList.Count > CurrentFrameCounter)
                         {
                              OutboundMessageQueue.Enqueue(CurrentMessage);
                              CurrentMessage = new MessageBody();
                         }
                    }
                    CurrentMessage = new MessageBody();
                    CurrentFrameCounter = 0;
                    CommState = new IdleState();
               }
          }
          public void RcvTimeout()
          {
               if (CommState is RcvWaitState)
               {
                    // Discard last incomplete message.
                    if (CurrentMessage.Terminator < 'E')
                    {
                         CurrentMessage = new MessageBody();
                    }
                    else
                    {
                         ProcessMessage(CurrentMessage);
                    }
                    // Return to idle state.
                    CommState = new IdleState();
               }
          }
          public void IdleCheck()
          {
               if (CommState is IdleState)
               {
                    if (OutboundMessageQueue.Count > 0)
                    {
                         HaveData();
                    }
               }
          }
     }
}
