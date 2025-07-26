namespace UniversaLIS.States
{
    public interface ILISState
    {
        /* This interface provides the template for each of the various 
         * operational states that the LIS will need. The methods below
         * will be defined differently for each derived class, according
         * to the behaviors required at each stage of operation.
         */
        void RcvInput(string InputString);
        void RcvENQ();
        void RcvACK();
        void RcvEOT();
        void RcvNAK();
        void RcvData(string InputString);
        void HaveData();
    }
}
