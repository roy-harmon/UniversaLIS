using static UniversaLIS.UniversaLIService;

namespace UniversaLIS.States
{

     class TransWaitState : ILISState
    {

        public TransWaitState(CommFacilitator comm)
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
            // If all frames have been sent, end the transmission.
            if (comm.CurrentMessage.FrameList.Count == comm.CurrentFrameCounter)
            {
                comm.Send(Constants.EOT);
                comm.CurrentMessage = comm.NewMessage();
            }
            else
            {
                // Otherwise, send next frame.
                comm.Send(comm.CurrentMessage.FrameList[comm.CurrentFrameCounter]);
                comm.CurrentFrameCounter++;
                // Reset the NAK count to 0.
                comm.NumNAK = 0;
                // Reset the transaction timer to 15 seconds.
                comm.TransTimer.Reset(15);
            }
        }

        public void RcvData(string InputString)
        {
            // Data frames should always be preceded by other signals, so for now, just log it.
            // Signal the instrument to interrupt the transmission with an EOT.
            AppendToLog("Data received in TransWait state: " + InputString);
            comm.Send(Constants.EOT);
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
            comm.Send(comm.CurrentMessage.FrameList[comm.CurrentFrameCounter - 1]);
            // Increment NAK count.
            comm.NumNAK++;
            if (comm.NumNAK == 6)
            {
                // Too many NAKs. Something's wrong. Send an EOT and go back to Idle.
                // Maybe stick the message back in the queue to try again later?
                comm.Send(Constants.EOT);
                comm.OutboundInstrumentMessageQueue.Enqueue(comm.CurrentMessage);
                comm.CurrentMessage = comm.NewMessage();
            }
        }

        void ILISState.HaveData()
        {
            // It doesn't matter if we have data to send. We're already sending something.
            AppendToLog("HaveData called in TransWait state.");
        }
    }
}
