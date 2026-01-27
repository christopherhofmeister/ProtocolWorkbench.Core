using FTD2XX_NET;
using ProtocolWorkBench.Core;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols;
using ProtocolWorkBench.Core.Protocols.Binary;
using ProtocolWorkBench.Core.Protocols.CBOR;
using ProtocolWorkBench.Core.Protocols.SMPCONSOLE;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using static ProtocolWorkBench.Core.Models.ProtocolDefinitions;
using static ProtocolWorkBench.Core.Models.UartFlowControl;

namespace ProtocolWorkbench.Core.Services.UartDevice
{
    public class UartDevice : SerialPort, IUartDevice
    {
        private Thread threadProcessIncomingSerialData;
        private bool processIncomingSerialData = false;
        private AutoResetEvent dataRxEvent;
        private const int SERIAL_READ_BUFFER_SIZE = 4096;
        private List<byte> incomingSerialDataBuffer;
        private int endOfMessageDetectionTimeMS;
        private const int SERIAL_READ_NUM_MISSED_BYTES = 5;
        private const int MAX_TX_ATTEMPTS = 10;
        private ConcurrentQueue<MessageBase> rxMessageQueue;
        private ConcurrentQueue<MessageBase> txMessageQueue;
        private bool writeInProgress = false;
        private ProtocolTypes Protocol;
        public event EventHandler RxMsgQueuedEvent;
        private event EventHandler TxMsgQueuedEvent;
        public ProcessJsonMessage processJsonMessage;
        public ProcessSmpMessage processSmpMessage;
        public ProcessBinaryMessage processBinaryMessage;
        public string UserAttribute1 { get; set; }
        public string UserAttribute2 { get; set; }
        public string FtdiSerialNumber { get; set; }
        public FTDI.FT_DEVICE FtdiDeviceType { get; set; }

        FlowControlTypes FlowControl { get; set; }

        public ReceivedMessageService ReceivedMessageService { get; private set; }
        public SMPoCMessageService SMPoCMessageService { get; set; }

        public bool DebugModeTx { get; set; }
        public bool DebugModeRx { get; set; }
        public bool DebugModeMsgProcess { get; set; }
        public bool SimulateUartRx { get; set; }
        public List<byte> SimulateUartRxData { get; set; }

        private int txAttemptCount;

        public UartDevice()
        {
            SerialPortInit();
        }

        public void SerialPortInit()
        {
            ReceivedMessageService = new ReceivedMessageService();
            rxMessageQueue = new ConcurrentQueue<MessageBase>();
            txMessageQueue = new ConcurrentQueue<MessageBase>();
            SimulateUartRxData = new List<byte>();
            dataRxEvent = new AutoResetEvent(false);
            incomingSerialDataBuffer = new List<byte>();
        }

        public void OnRxMsgQueuedEvent()
        {
            EventHandler eh = RxMsgQueuedEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        public void OnTxMsgQueuedEvent()
        {
            EventHandler eh = TxMsgQueuedEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Open COM Port and enable serial processing.
        /// </summary>
        /// <param name="comPort"></param>
        /// <param name="baudRate"></param>
        /// <param name="protocol"></param>
        /// <param name="flowControl"></param>
        public void EnableProcessingOnComPort(string comPort, int baudRate, ProtocolTypes protocol, FlowControlTypes flowControl)
        {
            DebugModeRx = true;
            ReceivedMessageService.SetReceivedMessageServiceComPort(comPort);
            Protocol = protocol;
            if (!SimulateUartRx)
            {
                OpenSerialPort(comPort, baudRate, flowControl);
            }
            endOfMessageDetectionTimeMS = CalculateEndOfMessageDetectionTimeMs(baudRate);
            StartProcessIncomimingSerialDataThread();
            StartProtocolSpecificProcessing(protocol);

            if (IsOpen)
            {
                this.DataReceived += UartService_DataReceived;
                TxMsgQueuedEvent += UartService_TxMsgQueuedEvent;
                DiscardInBuffer();
            }
        }
        private void UartService_TxMsgQueuedEvent(object sender, EventArgs e)
        {
            try
            {
                if (writeInProgress == false)
                {
                    MessageBase txMsg = new MessageBase();
                    if (true == txMessageQueue.TryDequeue(out txMsg))
                    {
                        if (false == SendData(txMsg.FullMessage))
                        {
                            txAttemptCount++;
                            if (txAttemptCount < MAX_TX_ATTEMPTS)
                            {
                                /* enqueue the message and try again */
                                EnqueueTxMessage(txMsg.FullMessage);
                                OnTxMsgQueuedEvent();
                            }
                            else
                            {
                                throw new Exception("Error!  Cannot EnqueueTxMessage");
                            }
                        }
                        else
                        {
                            txAttemptCount = 0;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Error TryDequeue Data on UART.");
                    }
                }
                else
                {
                    // check again
                    txAttemptCount++;
                    OnTxMsgQueuedEvent();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Dequeue a received message.
        /// </summary>
        /// <returns></returns>
        public (MessageBase MsgBase, bool Success) DequeueRxMessage()
        {
            bool success = false;
            MessageBase rxMsg = new MessageBase();
            if (true == rxMessageQueue.TryDequeue(out rxMsg))
            {
                rxMsg.ComPort = this.PortName;
                success = true;
            }
            return (rxMsg, success);
        }

        /// <summary>
        /// Add a Transmit Message to the Tx Queue
        /// </summary>
        /// <param name="txMsg"></param>
        /// <param name="valueType"></param>
        /// <param name="desiredAttributeResponseValue">Optional</param>
        public void EnqueueTxMessage(List<Byte> txMsg, string valueType = null, string desiredAttributeResponseValue = null)
        {
            MessageBase messageBase = new MessageBase();
            messageBase.FullMessage = txMsg;
            messageBase.TimeStamp = DateTime.Now;
            txMessageQueue.Enqueue(messageBase);
            OnTxMsgQueuedEvent();
        }

        /// <summary>
        /// Remove the two-byte CRC from the last two bytes of a message (send lsb first)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public (UInt16HbLb, List<byte>) RemoveCRCFromMessage(List<byte> message)
        {
            UInt16HbLb crc = new UInt16HbLb();
            /* Example data: 01 02 03 04
             * Count = 4
             * Index (04) = count - 1
             * Inex (03) = count - 2
             */
            crc.Lb = message[message.Count - 2];
            crc.Hb = message[message.Count - 1];

            message.RemoveRange(message.Count - 2, 2);

            return (crc, message);
        }

        /// <summary>
        /// Open COM Port at selected baud rate.
        /// </summary>
        /// <param name="comPort"></param>
        /// <param name="baudRate"></param>
        /// <param name="flowControl"></param>
        private void OpenSerialPort(string comPort, int baudRate, FlowControlTypes flowControl)
        {
            if (IsOpen)
            {
                Close();
            }

            // Try up to 3 times to open the COM port.  It is not garanteed
            // the COM port will close in a timely manner.  It may take multiple
            // tries to connect to the COM port because the resources have
            // not been released yet.
            // See: http://stackoverflow.com/questions/7348580/serialport-unauthorizedaccessexception
            int openTries = 0;
            bool comPortOpened = false;
            Exception openPortException = new Exception("Open COM Port");
            do
            {
                openTries++;
                try
                {
                    BaudRate = baudRate;
                    PortName = comPort;
                    Handshake = Handshake.None;
                    if (flowControl == FlowControlTypes.Rts)
                    {
                        Handshake = Handshake.RequestToSend;
                    }
                    else if (flowControl == FlowControlTypes.XonXoff)
                    {
                        Handshake = Handshake.XOnXOff;
                    }
                    else if (flowControl == FlowControlTypes.RtsXonXoff)
                    {
                        Handshake = Handshake.RequestToSendXOnXOff;
                    }
                    Open();
                    comPortOpened = true;
                }
                catch (Exception ex)
                {
                    openPortException = ex;
                    Debug.WriteLine(ex.ToString());
                    comPortOpened = false;
                    System.Threading.Thread.Sleep(300);
                }
            }
            while (openTries < 3 && !comPortOpened);
            if (!comPortOpened)
            {
                throw (openPortException);
            }
        }

        /// <summary>
        /// Send data on UART
        /// </summary>
        /// <param name="data"></param>
        /// <returns>false if the uart is not open, or another transmit is in progress</returns>
        private bool SendData(List<Byte> data)
        {
            if (!this.IsOpen)
            {
                Debug.WriteLine("Error!  Uart Not Open!");
                return false;
            }

            if (writeInProgress)
            {
                Debug.WriteLine("Error!  Write In Progress!");
                return false;
            }

            writeInProgress = true;
            this.Write(data.ToArray(), 0, data.Count);
            writeInProgress = false;
            if (DebugModeTx)
            {
                DebugService.PrintDebug(ConversionService.ConvertByteArrayToHexString(data, true), this.PortName, "TX");
            }
            return true;
        }

        /// <summary>
        /// Enable processing of messages based on protocol.
        /// </summary>
        /// <param name="protocol"></param>
        private void StartProtocolSpecificProcessing(ProtocolTypes protocol)
        {
            switch (protocol)
            {
                case ProtocolTypes.JSON:
                    processJsonMessage = new ProcessJsonMessage(this, this.ReceivedMessageService);
                    processJsonMessage.EnableProcessing();
                    break;
                case ProtocolTypes.SMP:
                    processSmpMessage = new ProcessSmpMessage(this, this.ReceivedMessageService);
                    processSmpMessage.EnableProcessing();
                    break;
                case ProtocolTypes.BINARYB:
                    processBinaryMessage = new ProcessBinaryMessage(this, this.ReceivedMessageService);
                    processBinaryMessage.EnableProcessing();
                    break;
                case ProtocolTypes.SMPCONSOLE:
                    SMPoCMessageService = new SMPoCMessageService(this);
                    SMPoCMessageService.EnableProcessing();
                    // set debug
                    SMPoCMessageService.OutputDebug = DebugModeMsgProcess;
                    CBORService.OutputDebug = DebugModeMsgProcess;
                    break;
                default:
                    throw new Exception("Error!  Unknown Serial Protocol.");
            }
        }

        /// <summary>
        /// Incoming data on uart event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UartService_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            dataRxEvent.Set();
        }

        /// <summary>
        /// Close the serial port.
        /// </summary>
        /// <param name="comPort"></param>
        public void CloseSerialPort()
        {
            StopProcessIncomimingSerialDataThread();
            DataReceived -= UartService_DataReceived;
            Close();
        }

        /// <summary>
        /// Calculates the time period after bytes are received to view the message as complete.
        /// </summary>
        /// <param name="baudRate"></param>
        /// <returns></returns>
        public int CalculateEndOfMessageDetectionTimeMs(int baudRate)
        {
            /* 1 / 230400 * 10 bits * 5 bytes = 0.2mS */

            decimal timePerBitSeconds = 1m / baudRate * 10m;
            decimal timeNoBytesSeconds = timePerBitSeconds * (decimal)SERIAL_READ_NUM_MISSED_BYTES;
            decimal timeNoBytesMs = Math.Round(timeNoBytesSeconds * 1000);
            /* windows system timer is 15mS, so can't go below that anyway */
            if (timeNoBytesMs < 15)
            {
                timeNoBytesMs = 15;
            }
            return (int)timeNoBytesMs;
        }

        /// <summary>
        /// Start serial receive processing thread
        /// </summary>
        private void StartProcessIncomimingSerialDataThread()
        {
            if (threadProcessIncomingSerialData == null)
            {
                threadProcessIncomingSerialData = new Thread(ProcessIncomimingSerialData);
                threadProcessIncomingSerialData.Priority = ThreadPriority.Highest;
                threadProcessIncomingSerialData.Name = "rxSerialDataThread";
                processIncomingSerialData = true;
                threadProcessIncomingSerialData.Start();
            }
        }

        /// <summary>
        /// Stop serial receive processing thread
        /// </summary>
        private void StopProcessIncomimingSerialDataThread()
        {
            processIncomingSerialData = false;
            dataRxEvent.Set();  // If we are on a wait one, force the loop to execute one last time
            if (threadProcessIncomingSerialData != null)
            {
                while (threadProcessIncomingSerialData.IsAlive)
                {
                    Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// Process incoming serial data.  After the timeout period the message will be added to the queue for processing
        /// </summary>
        private void ProcessIncomimingSerialData()
        {
            /* yes read whatever data is in the COM Port into local buffer */
            byte[] serialData = new byte[SERIAL_READ_BUFFER_SIZE];

            while (processIncomingSerialData)
            {
                if (SimulateUartRx)
                {
                    if (SimulateUartRxData.Count > 0)
                    {
                        Debug.WriteLine("Received Simulated RxData");
                        MessageBase messageBase = new MessageBase();
                        messageBase.TimeStamp = DateTime.Now;
                        messageBase.FullMessage.AddRange(SimulateUartRxData);
                        rxMessageQueue.Enqueue(messageBase);
                        Debug.WriteLine("Queue Count: " + rxMessageQueue.Count);
                        OnRxMsgQueuedEvent();
                        SimulateUartRxData.Clear();
                    }
                }
                else
                {
                    /* wait until a byte is received or timeout */
                    if (true == dataRxEvent.WaitOne(endOfMessageDetectionTimeMS))
                    {
                        if (this.IsOpen && this.BytesToRead > 0)
                        {
                            serialData = new byte[this.BytesToRead];
                            /* set to infinite timeout, blocks until data is ready */
                            int iCount = this.Read(serialData, 0, serialData.Length);
                            if (iCount > 0)
                            {
                                incomingSerialDataBuffer.AddRange(serialData);
                            }
                            else
                            {
                                dataRxEvent.Set();
                            }
                        }
                    }
                    else
                    {
                        /* timeout indicates end of message */
                        if (incomingSerialDataBuffer.Count > 0)
                        {
                            rxMessageQueue.Enqueue(new MessageBase { TimeStamp = DateTime.Now, FullMessage = incomingSerialDataBuffer });
                            OnRxMsgQueuedEvent();
                            if (DebugModeRx)
                            {
                                DebugService.PrintDebug(ConversionService.ConvertByteArrayToHexString(incomingSerialDataBuffer, true), this.PortName, "RX");
                            }
                            incomingSerialDataBuffer.Clear();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set Ftdi device to Async Bit Bang Mode
        /// </summary>
        /// <param name="mask">A bit value of 0 sets the corresponding pin to an input, a bit value of 1 sets the corresponding pin to an output</param>
        /// <returns>True on success, Error contain failure message</returns>
        public (bool Success, string Error) FtdiToAsyncBitBangMode(byte mask)
        {
            bool success = true;
            string errorMsg = null;

            // see:  https://ftdichip.com/wp-content/uploads/2020/08/D2XX_Programmers_GuideFT_000071.pdf
            FTDI ftdiLib = new FTDI();

            FTDI.FT_STATUS ftStatus = ftdiLib.OpenBySerialNumber(FtdiSerialNumber);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                ftStatus = ftdiLib.SetBitMode(mask, FTDI.FT_BIT_MODES.FT_BIT_MODE_ASYNC_BITBANG);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    success = false;
                    errorMsg = "Error!  Unable to set async bit bang mode on ftdi";
                }
            }
            else
            {
                success = false;
                errorMsg = "Error!  Unable to open FTDI Device";
            }
            ftdiLib.Close();
            return (success, errorMsg);
        }

        /// <summary>
        /// Set Ftdi device back to default (UART) mode.
        /// </summary>
        /// <returns>True on success, Error contain failure message</returns>
        public (bool Success, string Error) FtdiBitBangModeReset()
        {
            bool success = true;
            string errorMsg = null;
            // see:  https://ftdichip.com/wp-content/uploads/2020/08/D2XX_Programmers_GuideFT_000071.pdf
            FTDI ftdiLib = new FTDI();

            FTDI.FT_STATUS ftStatus = ftdiLib.OpenBySerialNumber(FtdiSerialNumber);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                ftStatus = ftdiLib.SetBitMode(0, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    success = false;
                    errorMsg = "Error!  Unable to reset async bit bang mode on ftdi";
                }
            }
            else
            {
                success = false;
                errorMsg = "Error!  Unable to open FTDI Device";
            }
            ftdiLib.Close();
            return (success, errorMsg);
        }

        /// <summary>
        /// Reset Ftdi device
        /// </summary>
        ///<returns>True on success, Error contain failure message</returns>
        public (bool Success, string Error) FtdiReset()
        {
            bool success = true;
            string errorMsg = null;
            // see:  https://ftdichip.com/wp-content/uploads/2020/08/D2XX_Programmers_GuideFT_000071.pdf
            FTDI ftdiLib = new FTDI();

            FTDI.FT_STATUS ftStatus = ftdiLib.OpenBySerialNumber(FtdiSerialNumber);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                ftStatus = ftdiLib.ResetDevice();
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    success = false;
                    errorMsg = "Error!  Unable to reset async bit bang mode on ftdi";
                }
            }
            else
            {
                success = false;
                errorMsg = "Error!  Unable to open FTDI Device";
            }
            ftdiLib.Close();
            return (success, errorMsg);
        }

        public void DiscardInputBuffer()
        {
            DiscardInBuffer();
        }

        public void DiscardOutputBuffer()
        {
            DiscardOutBuffer();
        }
    }
}
