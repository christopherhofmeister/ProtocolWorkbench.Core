using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames;

public sealed class BinaryFrameEncoder : IBinaryFrameEncoder
{
    public const byte SOF = 0xAA;
    public const byte EOF = 0x55;
    private const int LenFieldSize = 2;
    private const int TypeSize = 2;
    private const int FlagsSize = 1;
    private const int SeqSize = 4;
    private const int CrcSize = 2;

    // Everything after LEN, excluding payload
    private const int FixedOverheadAfterLen =
        TypeSize + FlagsSize + SeqSize + CrcSize + 1; // + EOF

    private readonly ICrcService _crc;

    public BinaryFrameEncoder(ICrcService crc)
    {
        _crc = crc ?? throw new ArgumentNullException(nameof(crc));
    }

    public byte[] Encode(BinaryFrame frame)
    {
        if (frame.Payload is null)
            throw new ArgumentNullException(nameof(frame.Payload));

        int payloadLen = frame.Payload.Length;

        // LEN = number of bytes AFTER LEN field
        ushort lenAfterLen = checked((ushort)(
            FixedOverheadAfterLen + payloadLen
        ));

        // Header = LEN + TYPE + FLAGS + SEQ
        Span<byte> header = stackalloc byte[
            LenFieldSize + TypeSize + FlagsSize + SeqSize
        ];

        int h = 0;
        WriteU16LE(header, h, lenAfterLen); h += LenFieldSize;
        WriteU16LE(header, h, frame.Type.U16Value); h += TypeSize;
        header[h++] = frame.Flags;
        WriteU32LE(header, h, frame.Seq);

        // CRC covers: LEN + TYPE + FLAGS + SEQ + PAYLOAD
        int crcInputLen = header.Length + payloadLen;
        byte[] crcInput = new byte[crcInputLen];

        header.CopyTo(crcInput); // include LEN now
        frame.Payload.CopyTo(crcInput.AsSpan(header.Length));

        UInt16HbLb crc16 = _crc.ComputeCcitt16(crcInput);

        int totalLen =
            1 +                 // SOF
            header.Length +
            payloadLen +
            CrcSize +
            1;                  // EOF

        byte[] buffer = new byte[totalLen];

        int o = 0;
        buffer[o++] = SOF;

        header.CopyTo(buffer.AsSpan(o));
        o += header.Length;

        if (payloadLen > 0)
        {
            frame.Payload.CopyTo(buffer.AsSpan(o));
            o += payloadLen;
        }

        buffer[o++] = crc16.Lb;
        buffer[o++] = crc16.Hb;

        buffer[o++] = EOF;

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