using static UniversaLIS.UniversaLIService;

namespace UniversaLIS.States
{

     public class LisCommState : ILISState
    {
        public ILISState CommState { get; set; }
        public CommFacilitator Comm { get; set; }
        public LisCommState(CommFacilitator comm)
        {
            this.Comm = comm;
            CommState = new IdleState(this.Comm);
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
            if (CommState is TransEnqState)
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
            if (CommState is TransWaitState && Comm.CurrentMessage.FrameList.Count < Comm.CurrentFrameCounter)
            {
                // When all frames have been sent, return to IdleState.
                ChangeToIdleState();
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
                ChangeToRcvWaitState();
            }
            if (CommState is TransEnqState)
            {
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
                if (Comm.OutboundInstrumentMessageQueue.Count > 0)
                {
                    // Don't make the operator wait for the timer tick.
                    IdleCheck();
                }
            }
        }

        public void RcvNAK()
        {
            CommState.RcvNAK();
            if (CommState is TransWaitState && Comm.NumNAK == 6)
            {
                ChangeToIdleState();
            }
            if (CommState is TransEnqState)
            {
                ChangeToIdleState();
            }
        }

        public void HaveData()
        {
            if (CommState is IdleState)
            {
                CommState.HaveData();
                // Change state
                ChangeToTransENQState();
            }
        }

        public void ChangeToIdleState()
        {
            CommState = new IdleState(Comm);
            Comm.CurrentFrameCounter = 0;
            Comm.TransTimer.Reset(-1);
        }
        public void ChangeToTransENQState()
        {
            CommState = new TransEnqState(Comm);
        }
        public void ChangeToTransWaitState()
        {
            CommState = new TransWaitState(Comm);
        }
        public void ChangeToRcvWaitState()
        {
            CommState = new RcvWaitState(Comm);
        }

        public void TransTimeout()
        {
            if (CommState is TransEnqState || CommState is TransWaitState)
            {
                // Send EOT and return to idle state.
                Comm.Send(Constants.EOT);
                if (CommState is TransWaitState && Comm.CurrentMessage.FrameList.Count > Comm.CurrentFrameCounter)
                {
                    Comm.OutboundInstrumentMessageQueue.Enqueue(Comm.CurrentMessage);
                    Comm.CurrentMessage = Comm.NewMessage();
                }
                Comm.CurrentMessage = Comm.NewMessage();
                Comm.CurrentFrameCounter = 0;
                ChangeToIdleState();
            }
        }
        public void RcvTimeout()
        {
            if (CommState is RcvWaitState)
            {
                // Discard last incomplete message.
                if (Comm.CurrentMessage.Terminator < 'E')
                {
                    Comm.CurrentMessage = Comm.NewMessage();
                }
                else
                {
                    Comm.ProcessMessage(Comm.CurrentMessage);
                }
                // Return to idle state.
                CommState = new IdleState(Comm);
            }
        }
        public void IdleCheck()
        {
            if (CommState is IdleState && Comm.OutboundInstrumentMessageQueue.Count > 0)
            {
                HaveData();
            }
        }
    }
}
