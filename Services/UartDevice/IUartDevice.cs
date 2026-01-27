using FTD2XX_NET;
using ProtocolWorkBench.Core;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.SMPCONSOLE;
using System;
using System.Collections.Generic;
using System.Text;
using static ProtocolWorkBench.Core.Models.ProtocolDefinitions;
using static ProtocolWorkBench.Core.Models.UartFlowControl;
using static ProtocolWorkBench.Core.Protocols.SMPCONSOLE.SMPoCMessageService;

namespace ProtocolWorkbench.Core.Services.UartDevice
{
    public interface IUartDevice
    {
        ReceivedMessageService ReceivedMessageService { get; }
        SMPoCMessageService SMPoCMessageService { get; set; }
        string UserAttribute1 { get; set; }
        string UserAttribute2 { get; set; }
        bool DebugModeTx { get; set; }
        bool DebugModeRx { get; set; }
        bool DebugModeMsgProcess { get; set; }
        string FtdiSerialNumber { get; set; }
        FTDI.FT_DEVICE FtdiDeviceType { get; set; }
        void EnableProcessingOnComPort(string comPort, int baudRate, ProtocolTypes protocol, FlowControlTypes flowControl);
        (MessageBase MsgBase, bool Success) DequeueRxMessage();

        // the desiredAttributeResponseValue and valueType are used in the MockService to send a specific response
        void EnqueueTxMessage(List<Byte> txMsg, string valueType = null, string desiredAttributeResponseValue = null);
        void OnRxMsgQueuedEvent();
        event EventHandler RxMsgQueuedEvent;
        string PortName { get; set; }
        void SerialPortInit();
        void CloseSerialPort();
        (bool Success, string Error) FtdiToAsyncBitBangMode(byte mask);
        (bool Success, string Error) FtdiBitBangModeReset();
        (bool Success, string Error) FtdiReset();
        void DiscardInputBuffer();
        void DiscardOutputBuffer();
        bool IsOpen { get; }
    }
}
