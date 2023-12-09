namespace UniversaLIS.States
{

    public class IdleState : ILISState
    {
        public IdleState(CommFacilitator comm)
        {
            this.comm = comm;
        }
        private readonly CommFacilitator comm;
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
            UniversaLIService.AppendToLog("ACK received in idle state...?");
        }

        public void RcvData(string InputString)
        {
            // Ignore... Possibly log the input for later reference?
            UniversaLIService.AppendToLog("Data received in idle state: " + InputString);
        }

        public void RcvENQ()
        {
            comm.Send(Constants.ACK);
        }

        public void RcvEOT()
        {
            // Ignore, but log it.
            UniversaLIService.AppendToLog("EOT received in idle state.");
        }

        public void RcvNAK()
        {
            // Ignore. It's just trying to get a rise out of you.
            UniversaLIService.AppendToLog("NAK received in idle state.");
        }

        public void HaveData()
        {
            // If there's data to send, check the timers before sending.
            if (comm.ContentTimer.RemainingDuration <= 0 && comm.BusyTimer.RemainingDuration <= 0)
            {
                // Send ENQ
                comm.Send(Constants.ENQ);
                // Set transTimer = 15
                comm.TransTimer.Reset(15);
            }
        }

    }
}
