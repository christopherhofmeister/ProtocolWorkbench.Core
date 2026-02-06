using ProtocolWorkbench.Core.Enums;
using System.Text;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers;

public static class BinaryFrameDebug
{
    private const int SofSize = 1;
    private const int LenSize = 2;
    private const int TypeSize = 2;
    private const int FlagsSize = 1;
    private const int SeqSize = 4;
    private const int CrcSize = 2;
    private const int EofSize = 1;

    private const byte SOF = 0xAA;
    private const byte EOF = 0x55;

    private const int HeaderAfterSofSize = LenSize + TypeSize + FlagsSize + SeqSize; // 9
    private const int FixedAfterLenOverhead = TypeSize + FlagsSize + SeqSize + CrcSize + EofSize; // 10

    public static string DecodeWire(byte[] bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return "Empty";

        var sb = new StringBuilder();
        sb.AppendLine($"Total: {bytes.Length} bytes");

        if (bytes.Length < SofSize)
            return sb.AppendLine("Missing SOF").ToString();

        byte sof = bytes[0];
        sb.AppendLine($"SOF: 0x{sof:X2}" + (sof == SOF ? "" : "  ⚠ Bad SOF"));

        // Minimum frame is payloadLen=0:
        // SOF(1) + LEN(2) + TYPE(2) + FLAGS(1) + SEQ(4) + CRC(2) + EOF(1) = 13
        int minFrame = SofSize + LenSize + FixedAfterLenOverhead;
        if (bytes.Length < minFrame)
        {
            sb.AppendLine($"Too short for minimum frame ({minFrame} bytes).");
            return sb.ToString();
        }

        ushort lenAfterLen = ReadU16LE(bytes, 1);
        sb.AppendLine($"LEN: {lenAfterLen}  (bytes after LEN)");

        int expectedTotal = SofSize + LenSize + lenAfterLen;

        if (bytes.Length < expectedTotal)
            sb.AppendLine($"⚠ Truncated: have {bytes.Length}, need {expectedTotal}");
        else if (bytes.Length > expectedTotal)
            sb.AppendLine($"⚠ Extra bytes after frame: have {bytes.Length}, frame is {expectedTotal}");

        ushort typeRaw = ReadU16LE(bytes, 3);
        byte categoryNibble = (byte)((typeRaw >> 12) & 0x0F);
        ushort msgType = (ushort)(typeRaw & 0x0FFF);

        var categoryName = Enum.IsDefined(typeof(BinaryCategory), categoryNibble)
            ? ((BinaryCategory)categoryNibble).ToString()
            : $"Unknown(0x{categoryNibble:X})";

        sb.AppendLine($"TYPE: 0x{typeRaw:X4}  (Cat=0x{categoryNibble:X} {categoryName}, Msg=0x{msgType:X3})");

        byte flagsRaw = bytes[5];

        // For [Flags] enums, Enum.IsDefined is usually NOT what you want (0x02,0x03,0x81 etc).
        // So we print the parsed flags string regardless.
        var flags = (Flags)flagsRaw;
        sb.AppendLine($"FLAGS: 0x{flagsRaw:X2}  ({flags})");

        uint seq = ReadU32LE(bytes, 6);
        sb.AppendLine($"SEQ: {seq}");

        // PayloadLen = LEN - (TYPE+FLAGS+SEQ+CRC+EOF)
        int payloadLen = lenAfterLen - FixedAfterLenOverhead;
        if (payloadLen < 0)
        {
            sb.AppendLine($"Invalid LEN: {lenAfterLen} smaller than fixed overhead {FixedAfterLenOverhead}.");
            return sb.ToString();
        }

        int payloadOffset = SofSize + HeaderAfterSofSize; // 1 + 9 = 10
        int crcOffset = payloadOffset + payloadLen;
        int eofOffset = crcOffset + CrcSize;

        sb.AppendLine($"Payload length: {payloadLen}");

        int payloadAvailable = Math.Max(0, Math.Min(payloadLen, bytes.Length - payloadOffset));
        var payload = payloadAvailable > 0
            ? bytes.Skip(payloadOffset).Take(payloadAvailable).ToArray()
            : Array.Empty<byte>();

        sb.AppendLine($"PAYLOAD: {(payload.Length == 0 ? "(none)" : BitConverter.ToString(payload).Replace("-", " "))}");

        if (bytes.Length >= crcOffset + CrcSize)
        {
            ushort crcWire = ReadU16LE(bytes, crcOffset);
            sb.AppendLine($"CRC (wire): 0x{crcWire:X4}  (bytes: {bytes[crcOffset]:X2} {bytes[crcOffset + 1]:X2})");
        }
        else
        {
            sb.AppendLine("CRC: (missing)");
        }

        if (bytes.Length >= eofOffset + 1)
        {
            byte eof = bytes[eofOffset];
            sb.AppendLine($"EOF: 0x{eof:X2}" + (eof == EOF ? "" : "  ⚠ Bad EOF"));
        }
        else
        {
            sb.AppendLine("EOF: (missing)");
        }

        return sb.ToString();
    }

    private static ushort ReadU16LE(byte[] buf, int offset)
        => (ushort)(buf[offset] | (buf[offset + 1] << 8));

    private static uint ReadU32LE(byte[] buf, int offset)
        => (uint)(buf[offset]
               | (buf[offset + 1] << 8)
               | (buf[offset + 2] << 16)
               | (buf[offset + 3] << 24));
}