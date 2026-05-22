using System.Formats.Cbor;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Core;

public sealed class McuMgrMessage
{
    public McuMgrHeader Header { get; }

    public byte[] Payload { get; }

    public McuMgrMessage(
        McuMgrHeader header,
        byte[] payload)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Payload = payload ?? Array.Empty<byte>();
    }

    public byte[] ToBytes()
    {
        byte[] headerBytes = Header.ToBytes();

        byte[] result = new byte[headerBytes.Length + Payload.Length];

        Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);

        if (Payload.Length > 0)
        {
            Buffer.BlockCopy(
                Payload,
                0,
                result,
                headerBytes.Length,
                Payload.Length);
        }

        return result;
    }

    public static McuMgrMessage Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < McuMgrHeader.HeaderLength)
        {
            throw new ArgumentException(
                "Buffer too small for MCUmgr message.");
        }

        McuMgrHeader header =
            McuMgrHeader.Parse(bytes[..McuMgrHeader.HeaderLength]);

        int payloadLength = header.Length;

        int expectedLength =
            McuMgrHeader.HeaderLength + payloadLength;

        if (bytes.Length < expectedLength)
        {
            throw new ArgumentException(
                $"Incomplete MCUmgr payload. Expected {expectedLength} bytes, got {bytes.Length}.");
        }

        byte[] payload =
            bytes
                .Slice(McuMgrHeader.HeaderLength, payloadLength)
                .ToArray();

        return new McuMgrMessage(header, payload);
    }

    public CborReader GetPayloadReader()
    {
        return new CborReader(Payload);
    }

    public override string ToString()
    {
        return $"MCUmgr Message: {Header}, PayloadLength={Payload.Length}";
    }
}