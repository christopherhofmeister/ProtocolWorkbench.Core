namespace ProtocolWorkbench.Core.Protocols.Binary.Models
{
    public readonly record struct MessageType(ushort Value)
    {
        public byte Category => (byte)((Value >> 12) & 0x0F);   // upper 4 bits
        public ushort Id => (ushort)(Value & 0x0FFF);           // lower 12 bits

        public static MessageType FromParts(byte category, ushort id)
        {
            if (category > 0x0F) throw new ArgumentOutOfRangeException(nameof(category));
            if (id > 0x0FFF) throw new ArgumentOutOfRangeException(nameof(id));
            return new MessageType((ushort)((category << 12) | id));
        }

        public override string ToString() => $"cat=0x{Category:X1} id=0x{Id:X3} (0x{Value:X4})";
    }
}
