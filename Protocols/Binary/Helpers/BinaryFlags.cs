namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class BinaryFlags
    {
        public const byte IsResponse = 0b0000_0001;
        public const byte IsNotification = 0b0000_0010;
        public const byte IsError = 0b0000_0100;
        public const byte AckRequested = 0b0000_1000;
    }
}
