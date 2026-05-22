namespace ProtocolWorkbench.Core.Protocols.McuMgr.Models
{
    public enum McuMgrErrorCode
    {
        Ok = 0,
        Unknown = 1,
        NoMemory = 2,
        InvalidArgument = 3,
        Timeout = 4,
        NoEntry = 5,
        BadState = 6,
        ResponseTooLarge = 7,
        NotSupported = 8,
        Corrupt = 9,
        Busy = 10,
        AccessDenied = 11,
        UnknownError = 256
    }
}
