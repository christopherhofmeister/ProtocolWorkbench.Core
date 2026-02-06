namespace ProtocolWorkbench.Core.Enums
{
    [Flags]
    public enum Flags : byte
    {
        None = 0,
        Request = 1 << 0, // 0x01
        Response = 1 << 1, // 0x02
        Notification = 1 << 2, // 0x04
        Error = 1 << 3, // 0x08
        Secure = 1 << 7, // 0x80 (reserved for later)
    }
}
