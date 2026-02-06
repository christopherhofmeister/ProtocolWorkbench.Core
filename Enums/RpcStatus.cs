namespace ProtocolWorkbench.Core.Enums
{
    public enum RpcStatus : byte
    {
        Ok = 0,
        // Fill these with your real meanings if you already have them:
        InvalidArg = 1,
        NotFound = 2,
        NotAllowed = 3,
        Busy = 4,
        Timeout = 5,
        InternalError = 6,
        Unknown = 7,
    }
}
