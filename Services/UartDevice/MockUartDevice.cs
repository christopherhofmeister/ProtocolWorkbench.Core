using FTD2XX_NET;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.McuMgr;
using ProtocolWorkBench.Core.Protocols.MCUMGR;
using ProtocolWorkBench.Core.Protocols.SMP.Models;
using ProtocolWorkBench.Core.Protocols.SMPCONSOLE;
using PeterO.Cbor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using static ProtocolWorkBench.Core.Models.ProtocolDefinitions;
using static ProtocolWorkBench.Core.Models.UartFlowControl;
using ProtocolWorkBench.Core;

namespace ProtocolWorkbench.Core.Services.UartDevice
{
    public class MockUartDevice : IUartDevice
    {
        private bool processingEnabled;
        private string comPort;
        private int baudRate;
        private ProtocolDefinitions.ProtocolTypes protocol;
        private FlowControlTypes flowControl;
        private ConcurrentQueue<MessageBase> rxMessageQueue;
        private string responseAttributeValue;
        private string responseAttributeType;
        public ReceivedMessageService ReceivedMessageService { get; private set; }
        public SMPoCMessageService SMPoCMessageService { get; set; }

        public string PortName { get; set; }
        public string UserAttribute1 { get; set; }
        public string UserAttribute2 { get; set; }
        public bool DebugModeTx { get; set; }
        public bool DebugModeRx { get; set; }
        public bool DebugModeMsgProcess { get; set; }
        public string FtdiSerialNumber { get; set; }
        public FTDI.FT_DEVICE FtdiDeviceType { get; set; }
        private System.Timers.Timer smpPacketTimeoutTimer;
        private List<byte> smpOverConsoleMessage;

        public event EventHandler RxMsgQueuedEvent;
        private SMPoCMockResponses sMPoCMockResponses;

        public MockUartDevice()
        {
            SerialPortInit();
            smpOverConsoleMessage = new List<byte>();
            smpPacketTimeoutTimer = new System.Timers.Timer(30);
            smpPacketTimeoutTimer.Elapsed += SmpPacketTimeoutTimer_Elapsed;
            smpPacketTimeoutTimer.AutoReset = false;
            sMPoCMockResponses = new SMPoCMockResponses();
        }

        private void SmpPacketTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            byte responseMgmtOp = 0;
            SmpMessage smpMsg = SMPoCService.SmpOverConsoleDefragmentFromBytes(smpOverConsoleMessage);
            smpOverConsoleMessage = new List<byte>();

            if (smpMsg.Header.GroupId.U16Value == McuMgrService.GroupIdOSMgmt)
            {
                if (smpMsg.Header.MessageId == McuMgrService.CommandIdMcuMgrParams)
                {
                    SendSMPoCMessages(sMPoCMockResponses.CreateMcuMgrParamsResponse(smpMsg));
                }
            }
            else if (smpMsg.Header.GroupId.U16Value == McuMgrService.GroupIdFileSystem)
            {
                if (smpMsg.Header.MessageId == McuMgrService.CommandIdFileSystem)
                {
                    if (smpMsg.Header.MgmtOp == (byte)McuMgrService.MgMtOpTypes.RequestRead)
                    {
                        SendSMPoCMessages(sMPoCMockResponses.CreateFileDownloadResponse(smpMsg));
                    }
                    else if (smpMsg.Header.MgmtOp == (byte)McuMgrService.MgMtOpTypes.RequestWrite)
                    {
                        SendSMPoCMessages(sMPoCMockResponses.CreateFileUploadResponse(smpMsg));
                    }
                }
                else if (smpMsg.Header.MessageId == McuMgrService.CommandIdFileStatus)
                {
                    SendSMPoCMessages(sMPoCMockResponses.CreateFileStatusResponse(smpMsg));
                }
                else if (smpMsg.Header.MessageId == McuMgrService.CommandIdFileSHA256)
                {
                    SendSMPoCMessages(sMPoCMockResponses.CreateFileSha256Response(smpMsg));
                }
            }
            else if (smpMsg.Header.GroupId.U16Value == McuMgrService.GroupIdShell)
            {
                if (smpMsg.Header.MessageId == McuMgrService.CommandIdShellExe)
                {
                    SendSMPoCMessages(sMPoCMockResponses.CreateShellExeResponse(smpMsg));
                }
            }
            else if (smpMsg.Header.GroupId.U16Value == McuMgrService.GroupIdApp)
            {
                if (smpMsg.Header.MessageId == McuMgrService.CommandIdGetParam)
                {
                    responseMgmtOp = (byte)McuMgrService.MgMtOpTypes.ResponseRead;
                    var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
                    string jsonRequest = obj1a.ToJSONString();
                    SendSMPoCMessages(sMPoCMockResponses.CreateAttributeGetorSetMessageResponse(smpMsg.Header, jsonRequest, responseMgmtOp,
                        responseAttributeType, responseAttributeValue));
                }
                else if (smpMsg.Header.MessageId == McuMgrService.CommandIdSetParam)
                {
                    responseMgmtOp = (byte)McuMgrService.MgMtOpTypes.ResponseWrite;
                    var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
                    string jsonRequest = obj1a.ToJSONString();
                    SendSMPoCMessages(sMPoCMockResponses.CreateAttributeGetorSetMessageResponse(smpMsg.Header, jsonRequest, responseMgmtOp));
                }
                else if (smpMsg.Header.MessageId == McuMgrService.CommandIdFactoryReset)
                {
                    SendSMPoCMessages(sMPoCMockResponses.CreateFactoryResetResponse(smpMsg));
                }
                else if (smpMsg.Header.MessageId == McuMgrService.CommandIdLoadParamFile)
                {
                    // If attribute is not empty send an error response back.
                    if (string.IsNullOrEmpty(UserAttribute1))
                    {
                        SendSMPoCMessages(sMPoCMockResponses.CreateParamLoadResponse(smpMsg, 0));
                    }
                    else
                    {
                        SendSMPoCMessages(sMPoCMockResponses.CreateParamLoadResponse(smpMsg, -1));
                    }
                }
                else if (smpMsg.Header.MessageId == McuMgrService.CommandIdDumpParamFile)
                {
                    // If attribute is not empty send an error response back.
                    if (string.IsNullOrEmpty(UserAttribute1))
                    {
                        SendSMPoCMessages(sMPoCMockResponses.CreateParamDumpResponse(smpMsg, 0));
                    }
                    else
                    {
                        SendSMPoCMessages(sMPoCMockResponses.CreateParamDumpResponse(smpMsg, -1));
                    }
                }
            }
        }

        public void SerialPortInit()
        {
            ReceivedMessageService = new ReceivedMessageService();
            rxMessageQueue = new ConcurrentQueue<MessageBase>();
        }

        public void CloseSerialPort()
        {
            processingEnabled = false;
            comPort = null;
            baudRate = 0;
            flowControl = FlowControlTypes.None;
            rxMessageQueue = new ConcurrentQueue<MessageBase>();
        }

        public (MessageBase, bool) DequeueRxMessage()
        {
            bool success = false;
            MessageBase rxMsg = new MessageBase();
            if (true == rxMessageQueue.TryDequeue(out rxMsg))
            {
                success = true;
            }
            return (rxMsg, success);
        }

        public void EnableProcessingOnComPort(string comPort, int baudRate, ProtocolTypes protocol, FlowControlTypes flowControl)
        {
            ReceivedMessageService.SetReceivedMessageServiceComPort(comPort);
            this.comPort = comPort;
            this.baudRate = baudRate;
            this.protocol = protocol;
            this.flowControl = flowControl;
            StartProtocolSpecificProcessing(protocol);
            processingEnabled = true;
        }

        private void StartProtocolSpecificProcessing(ProtocolTypes protocol)
        {
            switch (protocol)
            {
                case ProtocolTypes.SMPCONSOLE:
                    SMPoCMessageService = new SMPoCMessageService(this);
                    SMPoCMessageService.EnableProcessing();
                    break;
                default:
                    throw new Exception("Error!  Unknown Serial Protocol.");
            }
        }

        public void EnqueueTxMessage(List<byte> txMsg, string valueType = null, string desiredAttributeResponseValue = null)
        {
            if (processingEnabled)
            {
                if (protocol == ProtocolDefinitions.ProtocolTypes.SMPCONSOLE)
                {
                    // these packets may be fragmented, so wait until all fragments are received
                    // and then reassemble the packet and generate a response.
                    smpOverConsoleMessage.AddRange(txMsg);
                    responseAttributeValue = desiredAttributeResponseValue;
                    responseAttributeType = valueType;
                    smpPacketTimeoutTimer.Stop();
                    smpPacketTimeoutTimer.Start();
                }
            }
            else
            {
                throw new Exception($"Error!  Processing on COM {comPort} is not enabled.");
            }
        }

        public void OnRxMsgQueuedEvent()
        {
            EventHandler eh = RxMsgQueuedEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        public (bool Success, string Error) FtdiToAsyncBitBangMode(byte mask)
        {
            return (true, null);
        }

        public (bool Success, string Error) FtdiBitBangModeReset()
        {
            return (true, null);
        }

        public (bool Success, string Error) FtdiReset()
        {
            return (true, null);
        }

        public void DiscardInputBuffer()
        {

        }

        public void DiscardOutputBuffer()
        {

        }

        private void SendSMPoCMessages(List<List<byte>> messsages)
        {
            // If attribute is not empty do not create a response to simulate a response not coming back.
            if (string.IsNullOrEmpty(UserAttribute2))
            {
                foreach (var msg in messsages)
                {
                    MessageBase messageBase = new MessageBase();
                    messageBase.TimeStamp = DateTime.Now;
                    messageBase.FullMessage.AddRange(msg);
                    rxMessageQueue.Enqueue(messageBase);
                    OnRxMsgQueuedEvent();
                }
            }
        }

        public bool IsOpen { get; }
    }
}
