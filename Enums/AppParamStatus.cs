namespace ProtocolWorkbench.Core.Enums
{
    public enum AppParamStatus : byte
    {
        Ok = 0,
        InvalidArg = 1,
        NotFound = 2,
        ReadOnly = 3,
        WriteOnly = 4,
        RangeError = 5,
        InternalError = 6,
    }
}
