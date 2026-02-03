using Newtonsoft.Json;
using PeterO.Cbor;
using ProtocolWorkbench.Core.Services.Sha256Service;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.CBOR;
using ProtocolWorkBench.Core.Protocols.McuMgr;
using ProtocolWorkBench.Core.Protocols.MCUMGR;
using ProtocolWorkBench.Core.Protocols.SMP;
using ProtocolWorkBench.Core.Protocols.SMP.Models;
using static ProtocolWorkBench.Core.Protocols.McuMgr.McuMgrService;

namespace ProtocolWorkBench.Core.Protocols.SMPCONSOLE
{
    public class SMPoCMockResponses
    {
        public List<List<byte>> CreateAttributeGetorSetMessageResponse(SmpHeader smpHeader, string jsonRequest,
           byte mgmtOp, string valueType = null, object value = null)
        {
            dynamic d = JsonConvert.DeserializeObject(jsonRequest);
            UInt16 attributeId = d.p1;
            List<MessageParameter> msgParams = new List<MessageParameter>();
            if (mgmtOp == (byte)McuMgrService.MgMtOpTypes.ResponseRead)
            {
                msgParams = McuMgrService.GetAttributeResponseMsgParameters(attributeId, value, valueType);
            }
            else if (mgmtOp == (byte)McuMgrService.MgMtOpTypes.ResponseWrite)
            {
                msgParams = McuMgrService.SetAttributeResponseMsgParameters(attributeId, 0);
            }
            List<byte> response = McuMgrService.CreateSmpMessage(smpHeader.MessageId, msgParams, smpHeader.GroupId, mgmtOp);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(response);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateFileUploadResponse(SmpMessage smpMsg)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
            string jsonRequest = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(jsonRequest);
            UInt64 offset = (UInt64)d.off;
            int rc = 0;

            List<MessageParameter> msgParams = McuMgrService.FileUploadResponseMsgParameters(rc, offset);
            List<byte> response = McuMgrService.CreateSmpMessage(smpMsg.Header.MessageId, msgParams,
                smpMsg.Header.GroupId, (byte)McuMgrService.MgMtOpTypes.ResponseWrite);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(response);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateFileDownloadResponse(SmpMessage smpMsg)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
            string jsonRequest = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(jsonRequest);
            string sourceFile = d.name;

            if (!File.Exists(sourceFile))
            {
                throw new Exception($"Error!  Source File {sourceFile} doesn't exist at location.");
            }

            byte[] fileData = File.ReadAllBytes(sourceFile);

            CBORObject cborObj = CBORService.CreateCborByteStringForMcuMgrFileUploadResponse(fileData);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.ResponseRead, CommandIdFileSystem, FileSystemGroupId);
            List<byte> smpResponse = SMPService.CreateSmpMessage(cborObj, smpHeader);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(smpResponse);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateFileStatusResponse(SmpMessage smpMsg)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
            string jsonRequest = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(jsonRequest);
            string sourceFile = d.name;

            if (!File.Exists(sourceFile))
            {
                throw new Exception($"Error!  Source File {sourceFile} doesn't exist at location.");
            }

            byte[] fileData = File.ReadAllBytes(sourceFile);
            int rc = 0;

            List<MessageParameter> msgParams = McuMgrService.FileStatusResponseMsgParameters(rc, (UInt64)fileData.Length);
            List<byte> response = McuMgrService.CreateSmpMessage(smpMsg.Header.MessageId, msgParams,
                smpMsg.Header.GroupId, (byte)McuMgrService.MgMtOpTypes.ResponseRead);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(response);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateMcuMgrParamsResponse(SmpMessage smpMsg)
        {
            List<MessageParameter> msgParams = McuMgrService.McuMgrResponseMsgParameter(384, 4);
            List<byte> response = McuMgrService.CreateSmpMessage(smpMsg.Header.MessageId, msgParams,
                smpMsg.Header.GroupId, (byte)McuMgrService.MgMtOpTypes.ResponseRead);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(response);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateFileSha256Response(SmpMessage smpMsg, ISha256Service CRCService)
        {
            var obj1a = CBORObject.DecodeFromBytes(smpMsg.CBorMessage.ToArray());
            string jsonRequest = obj1a.ToJSONString();
            dynamic d = JsonConvert.DeserializeObject(jsonRequest);
            string sourceFile = d.name;

            if (!File.Exists(sourceFile))
            {
                throw new Exception($"Error!  Source File {sourceFile} doesn't exist at location.");
            }

            var result = CRCService.GenerateSHA256FromFile(sourceFile);
            byte[] fileData = File.ReadAllBytes(sourceFile);
            int rc = 0;

            CBORObject cborObj = CBORService.CreateCborByteStringForMcuMgrSha256Response(rc, McuMgrService.ChecksumTypeSha256,
                0, (UInt64)fileData.Length, result.SHA256);
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.ResponseRead,
                McuMgrService.CommandIdFileSHA256, FileSystemGroupId);
            List<byte> smpResponse = SMPService.CreateSmpMessage(cborObj, smpHeader);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(smpResponse);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateShellExeResponse(SmpMessage smpMsg)
        {
            int rc = 0;
            string output = "mock output";

            List<MessageParameter> msgParams = McuMgrService.ShellExeResponseMsgParameters(rc, output);
            List<byte> response = McuMgrService.CreateSmpMessage(smpMsg.Header.MessageId, msgParams,
                smpMsg.Header.GroupId, (byte)McuMgrService.MgMtOpTypes.ResponseWrite);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(response);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateFactoryResetResponse(SmpMessage smpMsg)
        {
            int rc = 0;
            string output = "mock output";

            List<MessageParameter> msgParams = McuMgrService.FactoryResetResponseMsgParameters();
            List<byte> response = McuMgrService.CreateSmpMessage(smpMsg.Header.MessageId, msgParams,
                smpMsg.Header.GroupId, (byte)McuMgrService.MgMtOpTypes.ResponseWrite);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(response);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateParamLoadResponse(SmpMessage smpMsg, int result)
        {
            CBORObject cborObj = CBORObject.NewMap();
            cborObj.Add("r", result);
            if (result < 0)
            {
                cborObj.Add("f", "/ext/feedback.txt");
            }
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.ResponseWrite, CommandIdLoadParamFile, AppGroupId);
            List<byte> smpResponse = SMPService.CreateSmpMessage(cborObj, smpHeader);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(smpResponse);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }

        public List<List<byte>> CreateParamDumpResponse(SmpMessage smpMsg, int result)
        {
            CBORObject cborObj = CBORObject.NewMap();
            cborObj.Add("r", result);
            if (result >= 0)
            {
                cborObj.Add("n", "/ext/dump.txt");
            }
            SmpHeader smpHeader = SMPService.FormatSmpHeader(cborObj, (byte)MgMtOpTypes.ResponseWrite, CommandIdDumpParamFile, AppGroupId);
            List<byte> smpResponse = SMPService.CreateSmpMessage(cborObj, smpHeader);
            List<byte> smpocLB = SMPoCService.SmpToSmpOverConsole(smpResponse);
            return SMPoCService.FragmentSMPOverConsoleMessage(smpocLB);
        }
    }
}
