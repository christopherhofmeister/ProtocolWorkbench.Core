namespace ProtocolWorkbench.Core.Protocols.McuMgr.Models
{
    public enum McuMgrOp : byte
    {
        Read = 0,
        ReadResponse = 1,
        Write = 2,
        WriteResponse = 3
    }
}
