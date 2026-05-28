namespace ProtocolWorkbench.Core.Enums
{
    public enum RpcStatus : byte
    {
        Ok = 0,
        NotFound = 1,
        NotReadable = 2,
        NotWritable = 3,
        InvalidArg = 4,
        BufferTooSmall = 5,
        InternalError = 6,
    }
}
