using static UniversaLIS.UniversaLIService;

namespace UniversaLIS.States
{

    class TransEnqState : ILISState
    {

        private readonly CommFacilitator comm;
        public TransEnqState(CommFacilitator comm)
        {
            this.comm = comm;
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
            // Send next frame.
            comm.CurrentMessage = comm.OutboundInstrumentMessageQueue.Dequeue();
            comm.CurrentMessage.PrepareToSend();
            comm.Send(comm.CurrentMessage.FrameList[comm.CurrentFrameCounter]);
            comm.CurrentFrameCounter++;
            // Reset the NAK count to 0.
            comm.NumNAK = 0;
            // Reset the transaction timer to 15 seconds.
            comm.TransTimer.Reset(15);
        }

        public void RcvData(string InputString)
        {
            // Ignore
            AppendToLog("Data received in TransENQ state: " + InputString);
        }

        public void RcvENQ()
        {
            // Set the contention timer to 20 seconds before returning to idle state.
            comm.ContentTimer.Reset(20);
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
            // If the instrument responds to our ENQ with NAK, it's busy. Back to Idle for 10 seconds.
            comm.BusyTimer.Reset(10);
        }

        void ILISState.HaveData()
        {
            // It doesn't matter if we have data to send. We're already trying to send.
            AppendToLog("HaveData called in TransENQ state.");
        }
    }
}
