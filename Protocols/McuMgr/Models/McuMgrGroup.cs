namespace ProtocolWorkbench.Core.Protocols.McuMgr.Models
{
    public enum McuMgrGroup : ushort
    {
        Os = 0,
        Image = 1,
        Stat = 2,
        Config = 3,
        Log = 4,
        Crash = 5,
        Split = 6,
        Run = 7,
        Fs = 8,
        // User-defined groups start at 64.
        User = 64
    }
}
