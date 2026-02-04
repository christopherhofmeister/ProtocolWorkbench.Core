namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class BinaryType
    {
        public static ushort Pack(byte category, ushort messageId)
        {
            if (category > 0x0F) throw new ArgumentOutOfRangeException(nameof(category));
            if (messageId > 0x0FFF) throw new ArgumentOutOfRangeException(nameof(messageId));
            return (ushort)((category << 12) | (messageId & 0x0FFF));
        }

        public static byte Category(ushort type) => (byte)((type >> 12) & 0x0F);
        public static ushort MessageId(ushort type) => (ushort)(type & 0x0FFF);
    }
}
