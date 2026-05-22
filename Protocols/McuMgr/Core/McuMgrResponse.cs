using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using System.Formats.Cbor;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Core;

public sealed class McuMgrResponse
{
    public McuMgrMessage Message { get; }

    public McuMgrHeader Header => Message.Header;

    public byte[] Payload => Message.Payload;

    public McuMgrOp Op => (McuMgrOp)Header.Op;

    public McuMgrGroup Group => (McuMgrGroup)Header.GroupId;

    public byte CommandId => Header.CommandId;

    public byte SequenceNumber => Header.SequenceNumber;

    public bool IsSuccess => ReturnCode == 0;

    public int ReturnCode { get; }

    public McuMgrResponse(McuMgrMessage message)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));

        ReturnCode = ParseReturnCode(message.Payload);
    }

    public static McuMgrResponse Parse(ReadOnlySpan<byte> bytes)
    {
        return new McuMgrResponse(
            McuMgrMessage.Parse(bytes));
    }

    public CborReader GetPayloadReader()
    {
        return new CborReader(Payload);
    }

    private static int ParseReturnCode(byte[] payload)
    {
        if (payload.Length == 0)
        {
            return -1;
        }

        try
        {
            var reader = new CborReader(payload);

            int? rc = null;

            int? length = reader.ReadStartMap();

            while (reader.PeekState() != CborReaderState.EndMap)
            {
                string key = reader.ReadTextString();

                if (key == "rc")
                {
                    rc = reader.ReadInt32();
                }
                else
                {
                    reader.SkipValue();
                }
            }

            reader.ReadEndMap();

            return rc ?? 0;
        }
        catch
        {
            return -1;
        }
    }

    public override string ToString()
    {
        return $"MCUmgr Response: Group={Group}, Id={CommandId}, Rc={ReturnCode}";
    }
}