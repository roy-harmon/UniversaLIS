using static IMMULIS.IMMULIService;

namespace IMMULIS
{
     public class IdleState : ILISState
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
               // Bask in the praise of the instrument? It's acknowledging us for no reason!
               // Seriously though, maybe we should add inappropriate incoming transmissions to the log file.
               AppendToLog("ACK received in idle state...?");
          }

          public void RcvData(string InputString)
          {
               // Ignore... Possibly log the input for later reference?
          }

          public void RcvENQ()
          {
               ComPort.Send(Constants.ACK);
          }

          public void RcvEOT()
          {
               // Ignore
          }

          public void RcvNAK()
          {
               // Ignore. It's just trying to get a rise out of you.
          }

          public void HaveData()
          {

               if (ContentTimer.remainingDuration <= 0 && BusyTimer.remainingDuration <= 0)
               {
                    // Send ENQ
                    ComPort.Send(Constants.ENQ);
                    // Set transTimer = 15
                    transTimer.Reset(15);
                    AppendToLog("Transaction timer reset: 15.");
               }
          }

     }
}
