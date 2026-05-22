namespace ProtocolWorkbench.Core.Protocols.McuMgr.Models
{
    public sealed class McuMgrUploadResult
    {
        public int Offset { get; set; }

        public int ReturnCode { get; set; }

        public bool IsSuccess => ReturnCode == 0;
    }
}
