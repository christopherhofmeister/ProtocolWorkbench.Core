using PeterO.Cbor;
using ProtocolWorkbench.Core.Enums;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.CBOR;
using ProtocolWorkBench.Core.Protocols.JSON;
using ProtocolWorkBench.Core.Protocols.MCUMGR;
using ProtocolWorkBench.Core.Protocols.SMP;
using static ProtocolWorkBench.Core.Models.MessageTypes;
using static ProtocolWorkBench.Core.Protocols.SMP.SMPService;

namespace ProtocolWorkBench.Core.Protocols.McuMgr
{
    public static class McuMgrService
    {
        public const string FileUpload = "file_upload";
        public const string FileDownload = "file_download";
        public const string GetParam = "get_parameter";
        public const string SetParam = "set_parameter";
        public const string SetRTC = "set_rtc";
        public const string GetRTC = "get_rtc";
        public const string LoadParmFile = "load_parameter_file";
        public const string DumpParmFile = "dump_parameter_file";
        public const string PrepareLog = "prepare_log";
        public const string AckLog = "ack_log";
        public const string FactoryReset = "factory_reset";
        public const string FileHashChecksum = "file_hash_checksum";
        public const string FileStatus = "file_status";
        public const string ShellExe = "shell_exec";

        public static UInt16HbLb OsMgmtSystemGroupId = new UInt16HbLb(0);
        public const int GroupIdOSMgmt = 0;
        public static UInt16HbLb FileSystemGroupId = new UInt16HbLb(8);
        public const int GroupIdFileSystem = 8;
        public static UInt16HbLb ShellGroupId = new UInt16HbLb(9);
        public const int GroupIdShell = 9;
        public static UInt16HbLb AppGroupId = new UInt16HbLb(65);
        public const int GroupIdApp = 65;
        public const string FileSystemParamOffset = "off";
        public const string FileSystemParamData = "data";
        public const string FileSystemParamLength = "len";
        public const string FileSystemParamName = "name";
        public const string FileSystemParamType = "type";
        public const string ChecksumTypeSha256 = "sha256";
        public const string ParamShellExeArgName = "argv";

        // keep command ids as bytes so that the application can override the default values if needed
        public static byte CommandIdFileSystem {get; set;}
        public static byte CommandIdGetParam { get; set; }
        public static byte CommandIdSetParam { get; set; }
        public static byte CommandIdFileStatus { get; set; }
        public static byte CommandIdFileSHA256 { get; set; }
        public static byte CommandIdShellExe { get; set; }
        public static byte CommandIdFactoryReset { get; set; }
        public static byte CommandIdLoadParamFile { get; set; }
        public static byte CommandIdDumpParamFile { get; set; }
        public static byte CommandIdMcuMgrParams { get; set; }


        public const byte CommandIdFileSystemDefault = 0;
        public const byte CommandIdGetParamDefault = 1;
        public const byte CommandIdSetParamDefault = 2;
        public const byte CommandIdFileStatusDefault =1;
        public const byte CommandIdFileSHA256Default = 2;
        public const byte CommandIdShellExeDefault = 0;
        public const byte CommandIdFactoryResetDefault = 5;
        public const byte CommandIdLoadParamFileDefault = 3;
        public const byte CommandIdDumpParamFileDefault = 4;
        public const byte CommandIdMcuMgrParamsDefault = 6;


        public const string P1 = "p1";
        public const string P2 = "p2";
        public const string Write = "Write";
        public const string Read = "Read";

        public enum DumpType
        {
            ReadWrite,
            Writeable,
            ReadOnly
        }

        static McuMgrService()
        {
            CommandIdFileSystem = CommandIdFileSystemDefault;
            CommandIdGetParam = CommandIdGetParamDefault;
            CommandIdSetParam = CommandIdSetParamDefault;
            CommandIdFileStatus = CommandIdFileStatusDefault;
            CommandIdFileSHA256 = CommandIdFileSHA256Default;
            CommandIdShellExe = CommandIdShellExeDefault;
            CommandIdFactoryReset = CommandIdFactoryResetDefault;
            CommandIdLoadParamFile = CommandIdLoadParamFileDefault;
            CommandIdDumpParamFile = CommandIdDumpParamFileDefault;
            CommandIdMcuMgrParams = CommandIdMcuMgrParamsDefault;
        }

        public enum MgMtOpTypes
        {
            RequestRead = 0,
            ResponseRead = 1,
            RequestWrite = 2,
            ResponseWrite = 3
        }

        /// <summary>
        /// Create message parameter for get param command
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static MessageParameter GetParamParameter(int id)
        {
            return new MessageParameter { Name = P1, Value = id.ToString(), CType = CTypes.UINT16 };
        }

        /// <summary>
        /// Create message parameter for set param command
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        private static List<MessageParameter> SetParamParameters(int id, object value, string valueType)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = P1, Value = id.ToString(), CType = CTypes.UINT16 };
            MessageParameter messageParameter2 = new MessageParameter { Name = P2, Value = value.ToString(), CType = valueType.ToString() };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            return msgParams;
        }

        /// <summary>
        /// Return SMP Messages for a file upload command based on the mtu size.
        /// sourceFilePath example:  "C:/temp/file1.txt"
        /// destFilePath example:  "/lsfs1/file1.txt"
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="destFilePath"></param>
        /// <param name="mtu"></param>
        /// <returns></returns>
        public static (List<List<byte>> Messages, UInt32 FileSize) McuMgrFileUploadRequest(string sourceFilePath, string destFilePath, UInt16 mtu)
        {
            string sampleLength = "\"len\": ";
            string sampleOffset = "\"off\": ";
            string sampleName = "\"name\": " + destFilePath;
            string sampleData = "\"data\": ";

            int lengthSize = sampleLength.Length + sizeof(UInt64);
            int lengthOffset = sampleOffset.Length + sizeof(UInt64);
            int lengthName = sampleName.Length;
            int lengthData = sampleData.Length;

            List<List<byte>> messages = new List<List<byte>>();
            int chunkSize = mtu - (SMPoCService.FRAME_OVERHEAD + SMPoCService.SMP_OVER_CONSOLE_HEADER_LENGTH_FIELD_LENGTH +
                lengthSize + lengthOffset + lengthName + lengthData);
            int offset = 0;
            UInt32 fileSize = 0;

            // read the file and convert to binary
            List<byte> fileData = File.ReadAllBytes(sourceFilePath).ToList();
            fileSize = (UInt32)fileData.Count;

            // the max size of an smpoc packet is defined by the host mtu.
            // from there the packet is sent in 127-byte chunks.
            while (fileData.Count > 0)
            {
                List<byte> msg = new List<byte>();
                // take chunk of message, or all of it
                if (fileData.Count > chunkSize)
                {
                    msg.AddRange(fileData.Take(chunkSize));
                    // remove the chunk we have just taken
                    fileData.RemoveRange(0, chunkSize);
                }
                else
                {
                    // Add the entire message since it is less than chunk size.
                    msg.AddRange(fileData);
                    // remove the chunk we have just taken
                    fileData.Clear();
                }

                CBORObject cborObj = CBORService.CreateCborByteStringForMcuMgrFileUpload(msg.ToArray(), destFilePath, offset, fileSize);
                // set the offset for the next transfer.
                offset += chunkSize;
                SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, CommandIdFileSystem, FileSystemGroupId);
                messages.Add(SMPService.CreateSmpMessage(cborObj, smpHeader));
            }
            return (messages, fileSize);
        }

        public static List<byte> McuMgrFileDownloadRequest(string sourceFilePath, UInt64 offset)
        {
            MessageParameter p1 = new MessageParameter { Name = FileSystemParamOffset, Value = offset.ToString(), CType = CTypes.UINT64 };
            MessageParameter p2 = new MessageParameter { Name = FileSystemParamName, Value = sourceFilePath, CType = CTypes.STRING };
            List<MessageParameter> p = new List<MessageParameter>();
            p.Add(p1);
            p.Add(p2);
            string json = JsonService.CreateJsonKeyValuePairObjects(p);
            CBORObject cborObj = CBORService.CreateCborFromJson(json);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestRead, CommandIdFileSystem, FileSystemGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> McuMgrFileHashChecksumRequest(string checkSumType, string sourceFilePath, UInt64 offset, UInt64 length)
        {
            MessageParameter p1 = new MessageParameter { Name = FileSystemParamType, Value = checkSumType, CType = CTypes.STRING };
            MessageParameter p2 = new MessageParameter { Name = FileSystemParamName, Value = sourceFilePath, CType = CTypes.STRING };
            MessageParameter p3 = new MessageParameter { Name = FileSystemParamOffset, Value = offset.ToString(), CType = CTypes.UINT64 };
            MessageParameter p4 = new MessageParameter { Name = FileSystemParamLength, Value = length.ToString(), CType = CTypes.UINT64 };
            List<MessageParameter> p = new List<MessageParameter>();
            p.Add(p1);
            p.Add(p2);
            p.Add(p3);
            p.Add(p4);
            string json = JsonService.CreateJsonKeyValuePairObjects(p);
            CBORObject cborObj = CBORService.CreateCborFromJson(json);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestRead, CommandIdFileSHA256, FileSystemGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> McuMgrFileStatusRequest(string sourceFilePath)
        {
            MessageParameter p1 = new MessageParameter { Name = FileSystemParamName, Value = sourceFilePath, CType = CTypes.STRING };
            List<MessageParameter> p = new List<MessageParameter>();
            p.Add(p1);
            string json = JsonService.CreateJsonKeyValuePairObjects(p);
            CBORObject cborObj = CBORService.CreateCborFromJson(json);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestRead, CommandIdFileStatus, FileSystemGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        /// <summary>
        /// Return a SMP Message for Get Attribute Command
        /// </summary>
        /// <param name="id"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public static List<byte> McuMgrGetAttributeRequest(byte attributeId, byte commandId, UInt16HbLb groupId)
        {
            MessageParameter p = GetParamParameter(attributeId);
            string json = JsonService.CreateJsonKeyValuePairObject(p);
            CBORObject cborObj = CBORService.CreateCborFromJson(json);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestRead, commandId, groupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        /// <summary>
        /// Return a SMP Message for Set Attribute Command
        /// </summary>
        /// <param name="id"></param>
        /// <param name="groupId"></param>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static List<byte> McuMgrSetAttributeRequest(byte attributeId, UInt16HbLb groupId, byte commandId, object value, string valueType)
        {
            List<MessageParameter> p = SetParamParameters(attributeId, value, valueType);
            string json = JsonService.CreateJsonKeyValuePairObjects(p);
            CBORObject cborObj = CBORService.CreateCborFromJson(json);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, commandId, groupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> McuMgrShellExeRequest(string singleShellCommand)
        {
            string[] commands = singleShellCommand.Split(' ');
            CBORObject cborObj = CBORService.CreateCborStringArray(ParamShellExeArgName, commands);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, CommandIdShellExe, ShellGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> McuMgrShellExeRequest(string[] commands)
        {
            CBORObject cborObj = CBORService.CreateCborStringArray(ParamShellExeArgName, commands);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, CommandIdShellExe, ShellGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> FactoryResetRequest()
        {
            CBORObject cborObj = CBORObject.NewMap();
            cborObj.Add("p1", 0);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, CommandIdFactoryReset, AppGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> LoadParamFileRequest(string remoteFilePath = null)
        {
            CBORObject cborObj = CBORObject.NewMap();
            if (!string.IsNullOrEmpty(remoteFilePath))
            {
                cborObj.Add("p1", remoteFilePath);
            }
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, CommandIdLoadParamFile, AppGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> DumpParamFileRequest(DumpType dumpType, string outputPath)
        {
            CBORObject cborObj = CBORObject.NewMap();
            cborObj.Add("p1", (byte)dumpType);
            if (!string.IsNullOrEmpty(outputPath))
            {
                cborObj.Add("p2", outputPath);
            }
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestWrite, CommandIdDumpParamFile, AppGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        public static List<byte> McuMgrParamsRequest()
        {
            CBORObject cborObj = CBORObject.NewMap();
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.RequestRead, CommandIdMcuMgrParams, OsMgmtSystemGroupId);
            return SMPService.CreateSmpMessage(cborObj, smpHeader);
        }

        /// <summary>
        /// This method will create a SMP message from msgParams
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="id"></param>
        /// <param name="msgParams"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public static List<byte> CreateSmpMessage(byte commandId, List<MessageParameter> msgParams,
            UInt16HbLb groupId, byte mgmtOp)
        {
            string json = null;
            List<byte> msgLByte = new List<byte>();
            json = JsonService.CreateJsonKeyValuePairObjects(msgParams);

            if (!string.IsNullOrEmpty(json))
            {
                CBORObject cborObj = CBORService.CreateCborFromJson(json);
                SmpHeader smpHeader = FormatSmpHeader(cborObj, mgmtOp, commandId, groupId);
                msgLByte = SMPService.CreateSmpMessage(cborObj, smpHeader);
            }

            return msgLByte;
        }


        /// <summary>
        /// Determine the MgmtOp from the API Setting
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="apiMgmtOption"></param>
        /// <returns></returns>
        public static MgMtOpTypes MgmtOptionFromApiToMgmtOp(MessageType messageType, string apiMgmtOption)
        {
            MgMtOpTypes mgMtOpType = MgMtOpTypes.RequestRead;

            if (messageType == MessageTypes.MessageType.Request)
            {
                if (apiMgmtOption == Read)
                {
                    mgMtOpType = MgMtOpTypes.RequestRead;
                }
                else if (apiMgmtOption == Write)
                {
                    mgMtOpType = MgMtOpTypes.RequestWrite;
                }
            }
            else if (messageType == MessageType.Response)
            {
                if (apiMgmtOption == Read)
                {
                    mgMtOpType = MgMtOpTypes.ResponseRead;
                }
                else if (apiMgmtOption == Write)
                {
                    mgMtOpType = MgMtOpTypes.ResponseWrite;
                }
            }

            return mgMtOpType;
        }

        /// <summary>
        /// Generate a get_parameter McuMgr Response
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static List<MessageParameter> GetAttributeResponseMsgParameters(int id, object value, string valueType)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "r", Value = value.ToString().Length.ToString(), CType = CTypes.UINT16 };
            MessageParameter messageParameter2 = new MessageParameter { Name = "id", Value = id.ToString(), CType = CTypes.UINT16 };
            MessageParameter messageParameter3 = new MessageParameter { Name = "r1", Value = value.ToString(), CType = valueType.ToString() };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            msgParams.Add(messageParameter3);
            return msgParams;
        }

        /// <summary>
        /// Generate a set_parameter McuMgr Response
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        public static List<MessageParameter> SetAttributeResponseMsgParameters(int id, UInt32 value)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "r", Value = value.ToString(), CType = CTypes.UINT32 };
            MessageParameter messageParameter2 = new MessageParameter { Name = "id", Value = id.ToString(), CType = CTypes.UINT16 };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            return msgParams;
        }

        /// <summary>
        /// Generate a file_download McuMgr Response
        /// </summary>
        /// <param name="rcValue"></param>
        /// <param name="offsetValue"></param>
        /// <returns></returns>
        public static List<MessageParameter> FileUploadResponseMsgParameters(int rcValue = 0, UInt64 offsetValue = 0)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "rc", Value = rcValue.ToString(), CType = CTypes.INT32 };
            MessageParameter messageParameter2 = new MessageParameter { Name = "off", Value = offsetValue.ToString(), CType = CTypes.UINT64 };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            return msgParams;
        }

        /// <summary>
        /// Generate a file_status McuMgr Response
        /// </summary>
        /// <param name="rcValue"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static List<MessageParameter> FileStatusResponseMsgParameters(int rcValue = 0, UInt64 length = 0)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "rc", Value = rcValue.ToString(), CType = CTypes.INT32 };
            MessageParameter messageParameter2 = new MessageParameter { Name = "len", Value = length.ToString(), CType = CTypes.UINT64 };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            return msgParams;
        }

        public static List<MessageParameter> McuMgrResponseMsgParameter(UInt32 bufferSize, UInt32 bufferCount)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "buf_size", Value = bufferSize.ToString(), CType = CTypes.UINT32 };
            MessageParameter messageParameter2 = new MessageParameter { Name = "buf_count", Value = bufferCount.ToString(), CType = CTypes.UINT32 };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            return msgParams;
        }

        public static List<MessageParameter> ShellExeResponseMsgParameters(int rcValue, string output)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "rc", Value = rcValue.ToString(), CType = CTypes.INT32 };
            MessageParameter messageParameter2 = new MessageParameter { Name = "o", Value = output, CType = CTypes.STRING };
            msgParams.Add(messageParameter1);
            msgParams.Add(messageParameter2);
            return msgParams;
        }

        public static List<MessageParameter> FactoryResetResponseMsgParameters(int result = 0)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();
            MessageParameter messageParameter1 = new MessageParameter { Name = "r", Value = result.ToString(), CType = CTypes.INT32 };
            msgParams.Add(messageParameter1);
            return msgParams;
        }
    }
}