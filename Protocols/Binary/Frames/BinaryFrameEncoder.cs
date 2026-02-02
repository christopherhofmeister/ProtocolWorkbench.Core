using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames;

public sealed class BinaryFrameEncoder : IBinaryFrameEncoder
{
    public const byte SOF = 0xAA;
    public const byte EOF = 0x55;

    private readonly ICrcService _crc;

    public BinaryFrameEncoder(ICrcService crc)
    {
        _crc = crc ?? throw new ArgumentNullException(nameof(crc));
    }

    public byte[] Encode(BinaryFrame frame)
    {
        if (frame.Payload is null)
            throw new ArgumentNullException(nameof(frame.Payload));

        // Header (LEN..SEQ): LEN(2) + TYPE(2) + FLAGS(1) + SEQ(4) = 9 bytes
        Span<byte> header = stackalloc byte[9];

        WriteU16LE(header, 0, frame.PayloadLength.U16Value);
        WriteU16LE(header, 2, frame.Type.U16Value);
        header[4] = frame.Flags;
        WriteU32LE(header, 5, frame.Seq);

        // CRC16 over: header + payload
        var crcInputLen = header.Length + frame.Payload.Length;
        byte[] crcInput = new byte[crcInputLen];

        header.CopyTo(crcInput.AsSpan(0, header.Length));
        frame.Payload.CopyTo(crcInput.AsSpan(header.Length));

        UInt16HbLb crc16 = _crc.ComputeCcitt16(crcInput);

        // Final: SOF + header + payload + CRC(2 LE) + EOF
        var totalLen = 1 + header.Length + frame.Payload.Length + 2 + 1;
        byte[] buffer = new byte[totalLen];

        int offset = 0;
        buffer[offset++] = SOF;

        header.CopyTo(buffer.AsSpan(offset));
        offset += header.Length;

        if (frame.Payload.Length > 0)
        {
            frame.Payload.CopyTo(buffer.AsSpan(offset));
            offset += frame.Payload.Length;
        }

        // CRC little-endian on wire (LSB first)
        buffer[offset++] = crc16.Lb;
        buffer[offset++] = crc16.Hb;

        buffer[offset++] = EOF;

        return buffer;
    }

    private static void WriteU16LE(Span<byte> buf, int offset, ushort value)
    {
        buf[offset] = (byte)(value & 0xFF);
        buf[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteU32LE(Span<byte> buf, int offset, uint value)
    {
        buf[offset] = (byte)(value & 0xFF);
        buf[offset + 1] = (byte)(value >> 8);
        buf[offset + 2] = (byte)(value >> 16);
        buf[offset + 3] = (byte)(value >> 24);
    }
}