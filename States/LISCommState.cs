using UniversaLIS.Models;
using static UniversaLIS.UniversaLIService;

namespace UniversaLIS.States
{

    public class LisCommState : ILISState
    {
        public ILISState CommState { get; set; }
        public CommFacilitator comm { get; set; }
        public LisCommState(CommFacilitator comm)
        {
            this.comm = comm;
            CommState = new IdleState(this.comm);
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
            if (CommState is TransWaitState && comm.CurrentMessage.FrameList.Count < comm.CurrentFrameCounter)
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
                if (comm.OutboundInstrumentMessageQueue.Count > 0)
                {
                    // Don't make the operator wait for the timer tick.
                    IdleCheck();
                }
            }
        }

        public void RcvNAK()
        {
            CommState.RcvNAK();
            if (CommState is TransWaitState && comm.NumNAK == 6)
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
#if DEBUG
                AppendToLog("HaveData complete.");
#endif
            }
        }

        public void ChangeToIdleState()
        {
            CommState = new IdleState(comm);
            comm.CurrentFrameCounter = 0;
            comm.TransTimer.Reset(-1);
#if DEBUG
            AppendToLog("CommState changed to IdleState!");
#endif
        }
        public void ChangeToTransENQState()
        {
            CommState = new TransEnqState(comm);
#if DEBUG
            AppendToLog("CommState changed to TransENQState!");
#endif
        }
        public void ChangeToTransWaitState()
        {
            CommState = new TransWaitState(comm);
#if DEBUG
            AppendToLog("CommState changed to TransWaitState!");
#endif
        }
        public void ChangeToRcvWaitState()
        {
            CommState = new RcvWaitState(comm);
#if DEBUG
            AppendToLog("CommState changed to RcvWaitState!");
#endif
        }

        public void TransTimeout()
        {
            if (CommState is TransEnqState || CommState is TransWaitState)
            {
                // Send EOT and return to idle state.
                comm.Send(Constants.EOT);
                if (CommState is TransWaitState && comm.CurrentMessage.FrameList.Count > comm.CurrentFrameCounter)
                {
                    comm.OutboundInstrumentMessageQueue.Enqueue(comm.CurrentMessage);
                    comm.CurrentMessage = new Message(comm);
                }
                comm.CurrentMessage = new Message(comm);
                comm.CurrentFrameCounter = 0;
                ChangeToIdleState();
            }
        }
        public void RcvTimeout()
        {
            if (CommState is RcvWaitState)
            {
                // Discard last incomplete message.
                if (comm.CurrentMessage.Terminator < 'E')
                {
                    comm.CurrentMessage = new Message(comm);
                }
                else
                {
                    comm.ProcessMessage(comm.CurrentMessage);
                }
                // Return to idle state.
                CommState = new IdleState(comm);
            }
        }
        public void IdleCheck()
        {
            if (CommState is IdleState && comm.OutboundInstrumentMessageQueue.Count > 0)
            {
                HaveData();
            }
        }
    }
}
