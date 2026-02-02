using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.CBOR;
using ProtocolWorkBench.Core.Protocols.McuMgr;
using ProtocolWorkBench.Core.Protocols.McuMgr.Models;
using ProtocolWorkBench.Core.Protocols.MCUMGR;
using ProtocolWorkBench.Core.Protocols.SMP;
using ProtocolWorkBench.Core.Protocols.SMP.Models;
using Newtonsoft.Json;
using PeterO.Cbor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static ProtocolWorkBench.Core.Protocols.McuMgr.McuMgrService;
using ProtocolWorkbench.Core.Services.UartDevice;
using ProtocolWorkbench.Core.Services.CrcService;

namespace ProtocolWorkBench.Core.Protocols.SMPCONSOLE
{
    public class SMPoCMessageService
    {
        private IUartDevice UartDevice;
        private System.Timers.Timer smpPacketTimeoutTimer;
        List<byte> smpOverConsoleMessage = new List<byte>();
        private List<List<byte>> FileUploadSmpMessages = new List<List<byte>>();
        private AutoResetEvent dataRxEvent;
        private const int UartResponseTimeoutMs = 3000;
        private SmpMessage ResposeMessage;
        private UInt32 FileUploadSize;
        private UInt32 FileSize;
        private UInt32 PacketSize;
        private string SourceFilePath;
        private int noEOFMarkerCount;
        private int eOFMarkerCount;
        private const int MAX_NO_EOF_COUNT = 50;
        private const int MAX_EOF_COUNT = 5;
        private const string RC = "rc";

        /// <summary>
        /// Full path of the downloaded file.
        /// </summary>
        public string FileDownloadPath { get; private set; }
        public List<byte> FileDownloadContents { get; private set; }
        public bool OutputDebug { get; set; }

        public event EventHandler<GetParamEventArgs> GetParamResponseEvent;
        public event EventHandler<SetParamEventArgs> SetParamResponseEvent;
        public event EventHandler<FileDownloadEventArgs> FileDownloadedEvent;
        public event EventHandler<FileUploadEventArgs> FileUploadedEvent;
        public event EventHandler<FileTransferEventArgs> FileTransferEvent;
        public event EventHandler<SmpMessage> TransmitEvent;
        public event EventHandler<SmpMessage> ReceiveEvent;
        public event EventHandler<Sha256EventArgs> Sha256ResponseEvent;
        public event EventHandler<FileStatusEventArgs> FileStatusResponseEvent;
        public event EventHandler<ShellExeEventArgs> ShellExeEvent;
        public event EventHandler<FactoryResetEventArgs> FactoryResetEvent;
        public event EventHandler<ParamLoadEventArgs> ParamLoadEvent;
        public event EventHandler<ParamDumpEventArgs> ParamDumpEvent;
        public event EventHandler<McuMgrParamsEventArgs> McuMgrParamsEvent;

        private enum SmpOCModes
        {
            Normal,
            FileUpload,
            FileDownload
        }

        private SmpOCModes SmpOverConsoleMode;

        public SMPoCMessageService()
        {
            FileDownloadContents = new List<byte>();
            ResposeMessage = new SmpMessage();
        }

        public SMPoCMessageService(IUartDevice uartDevice)
        {
            UartDevice = uartDevice;
            smpPacketTimeoutTimer = new System.Timers.Timer(30);
            smpPacketTimeoutTimer.Elapsed += SmpPacketTimeoutTimer_Elapsed;
            smpPacketTimeoutTimer.AutoReset = false;

            FileDownloadContents = new List<byte>();
            SmpOverConsoleMode = SmpOCModes.Normal;
            ResposeMessage = new SmpMessage();
            dataRxEvent = new AutoResetEvent(false);
        }

        #region Events
        public void OnGetParamResponseEvent(SmpHeader smpHeader, int result, UInt16 id, object value)
        {
            GetParamEventArgs args = new GetParamEventArgs(UartDevice.PortName, smpHeader, result, id, value);
            EventHandler<GetParamEventArgs> eh = GetParamResponseEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnSetParamResponseEvent(SmpHeader smpHeader, int result, UInt16 id)
        {
            SetParamEventArgs args = new SetParamEventArgs(UartDevice.PortName, smpHeader, result, id);
            EventHandler<SetParamEventArgs> eh = SetParamResponseEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnFileDownloadedEvent(string downloadPath)
        {
            if (UartDevice != null)
            {
                FileDownloadEventArgs args = new FileDownloadEventArgs(UartDevice.PortName, downloadPath);
                EventHandler<FileDownloadEventArgs> eh = FileDownloadedEvent;
                if (eh != null)
                {
                    eh(this, args);
                }
            }
        }

        public void OnFileUploadedEvent(UInt64 numBytes)
        {
            FileUploadEventArgs args = new FileUploadEventArgs(UartDevice.PortName, numBytes);
            EventHandler<FileUploadEventArgs> eh = FileUploadedEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnFileTransferEvent(UInt64 offset, UInt64 fileSize)
        {
            if (UartDevice != null)
            {
                FileTransferEventArgs args = new FileTransferEventArgs(UartDevice.PortName, offset, fileSize);
                EventHandler<FileTransferEventArgs> eh = FileTransferEvent;
                if (eh != null)
                {
                    eh(this, args);
                }
            }
        }

        public void OnSha256ResponseEvent(string sha256)
        {
            Sha256EventArgs args = new Sha256EventArgs(UartDevice.PortName, sha256);
            EventHandler<Sha256EventArgs> eh = Sha256ResponseEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnFileStatusResponseEvent(UInt64 fileLength)
        {
            FileStatusEventArgs args = new FileStatusEventArgs(UartDevice.PortName, fileLength);
            EventHandler<FileStatusEventArgs> eh = FileStatusResponseEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnShellExeResponseEvent(SmpHeader smpHeader, int rc, string output)
        {
            ShellExeEventArgs args = new ShellExeEventArgs(UartDevice.PortName, smpHeader, rc, output);
            EventHandler<ShellExeEventArgs> eh = ShellExeEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnFactoryResetEvent(SmpHeader smpHeader, int result)
        {
            FactoryResetEventArgs args = new FactoryResetEventArgs(UartDevice.PortName, smpHeader, result);
            EventHandler<FactoryResetEventArgs> eh = FactoryResetEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnParamLoadEvent(SmpHeader smpHeader, int result, string filePathErrors)
        {
            ParamLoadEventArgs args = new ParamLoadEventArgs(UartDevice.PortName, smpHeader, result, filePathErrors);
            EventHandler<ParamLoadEventArgs> eh = ParamLoadEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnParamDumpEvent(SmpHeader smpHeader, int result, string name)
        {
            ParamDumpEventArgs args = new ParamDumpEventArgs(UartDevice.PortName, smpHeader, result, name);
            EventHandler<ParamDumpEventArgs> eh = ParamDumpEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        public void OnTransmitEvent(SmpMessage smpMessage)
        {
            EventHandler<SmpMessage> eh = TransmitEvent;
            if (eh != null)
            {
                eh(this, smpMessage);
            }
        }

        public void OnMcuMgrParamsEvent(SmpHeader smpHeader, UInt32 bufferSize, UInt32 bufferCount)
        {
            McuMgrParamsEventArgs args = new McuMgrParamsEventArgs(UartDevice.PortName, smpHeader,
                bufferSize, bufferCount);
            EventHandler<McuMgrParamsEventArgs> eh = McuMgrParamsEvent;
            if (eh != null)
            {
                eh(this, args);
            }
        }

        /// <summary>
        /// This event is thrown on every smpMessage received except file upload/file download.
        /// It could be used by the app to receive all SMP messages and process responses outside this class.
        /// </summary>
        /// <param name="smpMessage"></param>
        public void OnReceiveEvent(SmpMessage smpMessage)
        {
            EventHandler<SmpMessage> eh = ReceiveEvent;
            if (eh != null)
            {
                eh(this, smpMessage);
            }
        }

        #endregion

        #region PublicApi

        public bool FileUploadRequest(string filePath, string destPath, UInt16 mtu)
        {
            bool success = false;

            // setup receive mode
            SetSmpOverConsoleReceiveMode(SmpOCModes.FileUpload);

            dataRxEvent.Reset();
            if (File.Exists(filePath))
            {
                var result = McuMgrService.McuMgrFileUploadRequest(filePath, destPath, mtu);
                FileUploadSmpMessages = result.Messages;
                FileUploadSize = result.FileSize;
                Task.Run(() => UploadFile());
                success = true;
            }
            return success;
        }

        public bool FileDownloadRequest(string sourcePath, string destPath)
        {
            bool success = false;
            FileDownloadContents = new List<byte>();
            SourceFilePath = sourcePath;
            if (!string.IsNullOrEmpty(sourcePath))
            {
                // setup receive mode
                SetSmpOverConsoleReceiveMode(SmpOCModes.FileDownload);

                FileDownloadPath = destPath;
                if (FileDownloadPath != null && File.Exists(FileDownloadPath))
                {
                    File.Delete(FileDownloadPath);
                }

                SendDownloadFileRequest(sourcePath, 0);
                success = true;
            }

            return success;
        }

        public void GetParameterRequest(byte attrId, byte commandId, UInt16HbLb groupId,
            string valueType = null, string desiredAttributeResponseValue = null)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrGetAttributeRequest(attrId, (byte)commandId, groupId);
#if MOCK
            SendSmpOCOverUart(smpMsg, valueType, desiredAttributeResponseValue);
#else
            SendSmpOCOverUart(smpMsg);
#endif
        }

        public void SetParameterRequest(byte attrId, byte commandId, UInt16HbLb groupId, object value, string cType)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrSetAttributeRequest(attrId, groupId, (byte)commandId, value, cType);
            SendSmpOCOverUart(smpMsg);
        }

        public void GetFileHashChecksumRequest(string checkSumType, string sourceFilePath, UInt64 offset, UInt64 length)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrFileHashChecksumRequest(checkSumType, sourceFilePath, offset, length);
            SendSmpOCOverUart(smpMsg);
        }

        public void GetFileStatusRequest(string sourceFilePath)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrFileStatusRequest(sourceFilePath);
            SendSmpOCOverUart(smpMsg);
        }

        public void ExecuteShellCommandRequest(string singleShellCommand)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrShellExeRequest(singleShellCommand);
            SendSmpOCOverUart(smpMsg);
        }

        public void ExecuteShellCommandRequest(string[] commands)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrShellExeRequest(commands);
            SendSmpOCOverUart(smpMsg);
        }

        public void FactoryResetRequest()
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.FactoryResetRequest();
            SendSmpOCOverUart(smpMsg);
        }

        public void LoadParamFileRequest(string remotePath = null)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.LoadParamFileRequest(remotePath);
            SendSmpOCOverUart(smpMsg);
        }

        public void DumpParmFileRequest(DumpType dumpType, string outPutPath = null)
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.DumpParamFileRequest(dumpType, outPutPath);
            SendSmpOCOverUart(smpMsg);
        }

        public void McuMgrParamsRequest()
        {
            SetSmpOverConsoleReceiveMode(SmpOCModes.Normal);
            List<byte> smpMsg = McuMgrService.McuMgrParamsRequest();
            SendSmpOCOverUart(smpMsg);
        }

        public void EnableProcessing()
        {
            UartDevice.RxMsgQueuedEvent += UartService_RxMsgQueuedEvent;
        }

#endregion

        #region PrivateMethods

        private void SmpPacketTimeoutTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // if the last byte is not an EOF byte (0x0a) we have not received the complete message.
            // if the last byte is an EOF byte, it still doesn't mean we have a complete message.
            // It could be that the packets is fragmented and a fragment happens to end with an EOF byte,
            // but there is more data to follow.
            if (smpOverConsoleMessage.Count > 0)
            {
                if (smpOverConsoleMessage[smpOverConsoleMessage.Count - 1] == SMPoCService.EOF)
                {
                    smpPacketTimeoutTimer.Stop();
                    eOFMarkerCount++;
                    noEOFMarkerCount = 0;
                    if (eOFMarkerCount == MAX_EOF_COUNT)
                    {
                        // No new data has come in, safe to process message.
                        eOFMarkerCount = 0;
                        ProcessSmpOverConsoleCompleteMessage();
                    }
                    else
                    {
                        // This packet does contain an EOF marker, but keep the timer going to see if more data is coming.
                        smpPacketTimeoutTimer.Start();
                    }
                }
                else
                {
                    noEOFMarkerCount++;
                    eOFMarkerCount = 0;
                    if (noEOFMarkerCount == MAX_NO_EOF_COUNT)
                    {
                        // Didn't get a full packet, throw it away.
                        noEOFMarkerCount = 0;
                        smpOverConsoleMessage.Clear();
                        smpPacketTimeoutTimer.Stop();
                        Debug.WriteLine("Did not receive full packet.  Timeout detected.  Discarding Packet.");
                    }
                    smpPacketTimeoutTimer.Start();
                }
            }
        }

        private void UartService_RxMsgQueuedEvent(object sender, EventArgs e)
        {
            var rxMessage = UartDevice.DequeueRxMessage();
            if (rxMessage.Success)
            {
                // dequeue uart message and add to current SMPoC message.
                // When the message timer expires the current message will be processed.
                smpOverConsoleMessage.AddRange(rxMessage.Item1.FullMessage);
                smpPacketTimeoutTimer.Stop();
                smpPacketTimeoutTimer.Start();
            }
        }

        private void ProcessSmpOverConsoleCompleteMessage()
        {
            if (smpOverConsoleMessage.Count > 0)
            {
                // handle the case where multiple messages SMPoC were sent by the device back to back.
                // convert the byte array to a byte string and then split on the header 06 09.
                // then convert each SMPoC message back to a byte array
                string msgstr = ConversionService.ConvertByteArrayToHexString(smpOverConsoleMessage, true);
                if (OutputDebug)
                {
                    DebugService.PrintDebug(msgstr, null, "SMPoC Msg To Process");
                }
                smpOverConsoleMessage = new List<byte>();
                List<List<byte>> lbMsgs = new List<List<byte>>();
                string[] messages = msgstr.Split(new string[] { "06 09" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string msg in messages)
                {
                    if (msg.Length > 0)
                    {
                        // add the delimiter back into the message
                        string orgMsg = "06 09 " + msg;
                        List<byte> lbMsg = ConversionService.ConvertHexByteStringWithSpacesToByteArray(orgMsg);
                        lbMsgs.Add(lbMsg);
                    }
                }

                switch (SmpOverConsoleMode)
                {
                    case SmpOCModes.Normal:
                        ProcessNormal(lbMsgs);
                        break;
                    case SmpOCModes.FileUpload:
                        ProcessFileUploadResponse(lbMsgs);
                        break;
                    case SmpOCModes.FileDownload:
                        ProcessFileDownloadResponse(lbMsgs);
                        break;
                }
            }
        }

        public SmpMessage ProcessFileUploadResponse(List<List<byte>> smpOverConsoleMessages)
        {
            SmpMessage smpMsg = new SmpMessage();
            foreach (var msg in smpOverConsoleMessages)
            {
                smpMsg = SMPoCService.SmpOverConsoleDefragmentFromBytes(msg);
                if (smpMsg.Header.GroupId.U16Value == McuMgrService.FileSystemGroupId.U16Value)
                {
                    var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
                    string json = obj1a.ToJSONString();
                    dynamic d = JsonConvert.DeserializeObject(json);
                    if (d.rc != 0)
                    {
                        DebugService.PrintDebug($"Error!  Response RC = {d.rc}");
                        break;
                    }
                    else
                    {
                        dataRxEvent.Set();
                        OnFileTransferEvent((UInt64)d.off, FileUploadSize);
                        DebugService.PrintDebug($"ProcessFileUploadResponse.  Offset = {d.off} Length = {FileUploadSize}");
                    }
                }
                else
                {
                    DebugService.PrintDebug($"Error!  Incorrect file system groupId {ResposeMessage.Header.GroupId.U16Value}!");
                    break;
                }
            }
            return smpMsg;
        }

        public SmpMessage ProcessFileDownloadResponse(List<List<byte>> smpOverConsoleMessages)
        {
            // the first message contains the SMP Header, subsequent messages do not.
            // subsequent messages to contain the SMPoC Header and CRC
            List<byte> message = new List<byte>();
            SmpMessage smpMsg = new SmpMessage();
            for (int i = 0; i < smpOverConsoleMessages.Count; i++)
            {
                if (i == 0)
                {
                    smpMsg = SMPoCService.SmpOverConsoleDefragmentFromBytes(smpOverConsoleMessages[0]);
                    PacketSize = smpMsg.Header.PayloadLength.U16Value;
                    message.AddRange(smpMsg.CBorMessage);
                }
                else
                {
                    message.AddRange(SMPoCService.FileDownloadFragmentToBytes(smpOverConsoleMessages[i]));
                }
            }
            if ((PacketSize == message.Count) && (message.Count != 0))
            {
                var obj1a = CBORObject.DecodeFromBytes(message.ToArray());
                string json = obj1a.ToJSONString();
                var result = CBORService.ProcessCborObject(obj1a);
                FileDownloadContents.AddRange(result.ByteString);

                dynamic d = JsonConvert.DeserializeObject(json);
                UInt64 offset = d.off;
                if (offset == 0)
                {
                    FileSize = d.len;
                }
                DebugService.PrintDebug($"ProcessFileDownloadResponse.  Offset = {d.off} Length = {FileSize}");
                OnFileTransferEvent(offset, FileSize);

                if (FileSize > FileDownloadContents.Count)
                {
                    SendDownloadFileRequest(SourceFilePath, (UInt64)FileDownloadContents.Count);
                }
                else
                {
                    if (!string.IsNullOrEmpty(FileDownloadPath))
                    {
                        SaveFileAsBinary(FileDownloadPath, FileDownloadContents);
                    }
                    OnFileTransferEvent((UInt64)FileDownloadContents.Count, FileSize);
                    OnFileDownloadedEvent(FileDownloadPath);
                }
            }
            else
            {
                DebugService.PrintDebug("Error!  Incorrect SMPoC Length.  Throwing away packet.");
            }
            return smpMsg;
        }

        private void ProcessFileHashChecksumResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            var result = CBORService.ProcessCborObject(obj1a);
            string sha256 = CRCService.Sha256BytesToString(result.ByteString.ToArray());
            OnSha256ResponseEvent(sha256);
        }

        private void ProcessFileStatusResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            var result = CBORService.ProcessCborObject(obj1a);
            dynamic d = JsonConvert.DeserializeObject(json);
            OnFileStatusResponseEvent((UInt64)d.len);
        }

        private void ProcessMcuMgrParamsResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            var result = CBORService.ProcessCborObject(obj1a);
            dynamic d = JsonConvert.DeserializeObject(json);
            if (json.Contains(RC))
            {
                OnMcuMgrParamsEvent(smpMessage.Header, 0, 0);
                Debug.WriteLine($"RC = {d.rc} on {UartDevice.PortName}");
            }
            else
            {
                OnMcuMgrParamsEvent(smpMessage.Header, (UInt32)d.buf_size, (UInt32)d.buf_count);
            }
        }

        private void ProcessGetParamResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            if (json.Contains(RC))
            {
                OnGetParamResponseEvent(smpMessage.Header, (int)d.rc, 0, null);
            }
            else
            {
                OnGetParamResponseEvent(smpMessage.Header, (int)d.r, (UInt16)d.id, (object)d.r1);
            }
        }

        private void ProcessSetParamResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            if (json.Contains(RC))
            {
                OnSetParamResponseEvent(smpMessage.Header, (int)d.rc, 0);
            }
            else
            {
                OnSetParamResponseEvent(smpMessage.Header, (int)d.r, (UInt16)d.id);
            }
        }

        private void ProcessShellExeResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            OnShellExeResponseEvent(smpMessage.Header, (int)d.rc, (string)d.o);
        }

        private void ProcessFactoryResetResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            OnFactoryResetEvent(smpMessage.Header, 0);
        }

        private void ProcessParamLoadResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            int result = (int)d.r;
            string errorFile = null;
            if (result < 0)
            {
                errorFile = (string)d.f;
            }
            OnParamLoadEvent(smpMessage.Header, result, errorFile);
        }

        private void ProcessParamDumpResponse(SmpMessage smpMessage)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMessage.CBorMessage.ToArray());
            string json = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(json);
            int result = (int)d.r;
            string nameFile = null;
            if (result >= 0)
            {
                nameFile = (string)d.n;
            }
            OnParamDumpEvent(smpMessage.Header, result, nameFile);
        }

        private void ProcessNormal(List<List<byte>> smpOverConsoleMessages)
        {
            foreach (var msg in smpOverConsoleMessages)
            {
                SmpMessage smpMsg = SMPoCService.SmpOverConsoleDefragmentFromBytes(msg);
                OnReceiveEvent(smpMsg);
                UartDevice.ReceivedMessageService.AddMessageToQueue(smpMsg);
                ProcessSmpResponse(smpMsg);
            }
        }

        public SmpMessage ProcessNormal(List<byte> smpOverConsoleMessage)
        {
            SmpMessage smpMsg = SMPoCService.SmpOverConsoleDefragmentFromBytes(smpOverConsoleMessage);
            ProcessSmpResponse(smpMsg);
            return smpMsg;
        }

        private void ProcessSmpResponse(SmpMessage smpMessage)
        {
            switch (smpMessage.Header.GroupId.U16Value)
            {

                case McuMgrService.GroupIdOSMgmt:
                    if (smpMessage.Header.MessageId == CommandIdMcuMgrParams)
                    {
                        ProcessMcuMgrParamsResponse(smpMessage);
                    }
                    break;

                case McuMgrService.GroupIdFileSystem:
                    if (smpMessage.Header.MessageId == CommandIdFileSHA256)
                    {
                        ProcessFileHashChecksumResponse(smpMessage);
                    }
                    else if (smpMessage.Header.MessageId == CommandIdFileStatus)
                    {
                        ProcessFileStatusResponse(smpMessage);
                    }
                    break;

                case McuMgrService.GroupIdShell:
                    if (smpMessage.Header.MessageId == McuMgrService.CommandIdShellExe)
                    {
                        ProcessShellExeResponse(smpMessage);
                    }
                    break;

                case McuMgrService.GroupIdApp:
                    if (smpMessage.Header.MessageId == McuMgrService.CommandIdGetParam)
                    {
                        ProcessGetParamResponse(smpMessage);
                    }
                    else if (smpMessage.Header.MessageId == McuMgrService.CommandIdSetParam)
                    {
                        ProcessSetParamResponse(smpMessage);
                    }
                    else if (smpMessage.Header.MessageId == McuMgrService.CommandIdFactoryReset)
                    {
                        ProcessFactoryResetResponse(smpMessage);
                    }
                    else if (smpMessage.Header.MessageId == McuMgrService.CommandIdLoadParamFile)
                    {
                        ProcessParamLoadResponse(smpMessage);
                    }
                    else if (smpMessage.Header.MessageId == McuMgrService.CommandIdDumpParamFile)
                    {
                        ProcessParamDumpResponse(smpMessage);
                    }
                    break;
            }
        }

        /// <summary>
        /// Set the receive mode needs to be set before file upload and file download.
        /// During the file upload MTU chunks are acknowledged by the device, and there is no need to notify the application.
        /// During the file download the SMP Header is only sent in the first packet.  Subsequent packets only have a SMPoC
        /// header, which essentially needs to be treated as a different protocol. This has been brought to the attention
        /// of the firmware team, and maybe something to fix in the future for them.
        /// A bit of a headache for the host parser as its a special case to handle and no clean way to handle it.
        /// </summary>
        /// <param name="smpOCModes"></param>
        private void SetSmpOverConsoleReceiveMode(SmpOCModes smpOCMode)
        {
            SmpOverConsoleMode = smpOCMode;
        }

        private void UploadFile()
        {
            bool success = true;
            foreach (List<byte> smpMsg in FileUploadSmpMessages)
            {
                SmpMessage smpMessage = SMPService.ConvertRawBytesToSmpMessage(smpMsg);
                OnTransmitEvent(smpMessage);

                dataRxEvent.Reset();
                List<byte> smpOverConsoleMsg = SMPoCService.SmpToSmpOverConsole(smpMsg);
                List<List<byte>> msgs = SMPoCService.FragmentSMPOverConsoleMessage(smpOverConsoleMsg);
                foreach (var msg in msgs)
                {
                    UartDevice.EnqueueTxMessage(msg);
                }

                if (false == dataRxEvent.WaitOne(UartResponseTimeoutMs))
                {
                    success = false;
                    break;
                }
            }
            if (success)
            {
                // if sent all packets & had a response to all packets
                OnFileUploadedEvent(FileUploadSize);
            }
        }

        private void SendDownloadFileRequest(string sourcePath, UInt64 offset)
        {
            List<byte> smpMsg = McuMgrService.McuMgrFileDownloadRequest(sourcePath, offset);
            SmpMessage smpMessage = SMPService.ConvertRawBytesToSmpMessage(smpMsg);
            OnTransmitEvent(smpMessage);
            List<byte> smpOverConsoleMsg = SMPoCService.SmpToSmpOverConsole(smpMsg);
            List<List<byte>> msgs = SMPoCService.FragmentSMPOverConsoleMessage(smpOverConsoleMsg);
            foreach (var msg in msgs)
            {
                UartDevice.EnqueueTxMessage(msg);
            }
        }

        private void SaveFileAsBinary(string filePath, List<byte> data)
        {
            using (BinaryWriter binWriter = new BinaryWriter(File.Open(filePath, FileMode.Create)))
            {
                binWriter.Write(data.ToArray());
            }
        }

        private void SendSmpOCOverUart(List<byte> smpMsg, string valueType = null, string desiredAttributeResponseValue = null)
        {
            // convert to smp class and throw event
            SmpMessage smpMessage = SMPService.ConvertRawBytesToSmpMessage(smpMsg);
            OnTransmitEvent(smpMessage);

            // convert to smp-over-console and handle fragmentation, then send
            List<byte> smpOverConsoleMsg = SMPoCService.SmpToSmpOverConsole(smpMsg);
            List<List<byte>> msgs = SMPoCService.FragmentSMPOverConsoleMessage(smpOverConsoleMsg);
            foreach (var msg in msgs)
            {
#if MOCK
                UartDevice.EnqueueTxMessage(msg, valueType, desiredAttributeResponseValue);
#else
                UartDevice.EnqueueTxMessage(msg);
#endif

            }
        }

#endregion
    }
}
