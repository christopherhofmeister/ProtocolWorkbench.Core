using ProtocolWorkbench.Core.Protocols.McuMgr.Models;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Core;

public sealed class McuMgrRequest
{
    public McuMgrOp Op { get; }
    public McuMgrGroup Group { get; }
    public byte CommandId { get; }
    public byte SequenceNumber { get; }
    public byte[] Payload { get; }

    public McuMgrRequest(
        McuMgrOp op,
        McuMgrGroup group,
        byte commandId,
        byte sequenceNumber,
        byte[]? payload = null)
    {
        Op = op;
        Group = group;
        CommandId = commandId;
        SequenceNumber = sequenceNumber;
        Payload = payload ?? Array.Empty<byte>();
    }

    public McuMgrMessage ToMessage()
    {
        var header = new McuMgrHeader(
            op: (byte)Op,
            flags: 0,
            length: checked((ushort)Payload.Length),
            groupId: (ushort)Group,
            sequenceNumber: SequenceNumber,
            commandId: CommandId);

        return new McuMgrMessage(header, Payload);
    }

    public byte[] ToBytes()
    {
        return ToMessage().ToBytes();
    }
}