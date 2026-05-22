namespace ProtocolWorkbench.Core.Protocols.McuMgr.Core;

public sealed class McuMgrHeader
{
    public const int HeaderLength = 8;

    public byte Op { get; }
    public byte Flags { get; }
    public ushort Length { get; }
    public ushort GroupId { get; }
    public byte SequenceNumber { get; }
    public byte CommandId { get; }

    public McuMgrHeader(
        byte op,
        byte flags,
        ushort length,
        ushort groupId,
        byte sequenceNumber,
        byte commandId)
    {
        Op = op;
        Flags = flags;
        Length = length;
        GroupId = groupId;
        SequenceNumber = sequenceNumber;
        CommandId = commandId;
    }

    public byte[] ToBytes()
    {
        return
        [
            Op,
            Flags,
            (byte)(Length >> 8),
            (byte)Length,
            (byte)(GroupId >> 8),
            (byte)GroupId,
            SequenceNumber,
            CommandId
        ];
    }

    public static McuMgrHeader Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength)
        {
            throw new ArgumentException(
                $"MCUmgr SMP header requires {HeaderLength} bytes, got {bytes.Length}.");
        }

        return new McuMgrHeader(
            op: bytes[0],
            flags: bytes[1],
            length: (ushort)((bytes[2] << 8) | bytes[3]),
            groupId: (ushort)((bytes[4] << 8) | bytes[5]),
            sequenceNumber: bytes[6],
            commandId: bytes[7]);
    }

    public override string ToString()
    {
        return $"MCUmgr Header: Op={Op}, Flags={Flags}, Len={Length}, Group={GroupId}, Seq={SequenceNumber}, Id={CommandId}";
    }
}