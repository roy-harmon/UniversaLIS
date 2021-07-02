using System;
using System.IO.Ports;

namespace IMMULIS
{
     /* 
      *   Code adapted from example by Valentin Gies found at https://www.vgies.com/a-reliable-serial-port-in-c/,
      *   as well as Ben Voigt's article at https://www.sparxeng.com/blog/software/must-use-net-system-io-ports-serialport.
      *   Thanks, Ben and Valentin!
      */
     public class ReliableSerialPort : SerialPort
    {
        public ReliableSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
            Handshake = Handshake.None;
            DtrEnable = true;
            NewLine = Environment.NewLine;
            ReceivedBytesThreshold = 1024;
        }

        new public void Open()
        {
            base.Open();
            ContinuousRead();
        }

        /* 
         * Infinitely loops through kickoffRead method
         */
        private void ContinuousRead()
        {
            byte[] buffer = new byte[4096];
            Action kickoffRead = null;
            //read incoming message and store in buffer
            kickoffRead = (Action)(() => BaseStream.BeginRead(buffer, 0, buffer.Length, delegate (IAsyncResult ar)
            {
                //size of the message being recieved
                //the size is anywhere between 0 and the size of the buffer variable
                int count = BaseStream.EndRead(ar);
                //create a byte array the size of the message being recieved
                byte[] dst = new byte[count];
                // copy relevant portion of buffer (the message) into dst so there is no extra space in the array
                Buffer.BlockCopy(buffer, 0, dst, 0, count);
                //store message
                OnDataReceived(dst);
                // loop
                kickoffRead();
            }, null)); kickoffRead();
        }

        public new event EventHandler<DataReceivedArgs> DataReceived;
        public virtual void OnDataReceived(byte[] data)
        {
               DataReceived?.Invoke(this, new DataReceivedArgs { Data = data });
          }
    }

    public class DataReceivedArgs : EventArgs
    {
        public byte[] Data { get; set; }
    }
}
