using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using System.Security.Cryptography;

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

    public byte[] EncodeSecureChaCha20Poly1305(BinaryFrame frame, byte[] key32, byte[] nonceBase12)
    {
        if (frame.Payload is null)
            throw new ArgumentNullException(nameof(frame.Payload));

        byte[] nonce = BuildNonce(nonceBase12, frame.Seq);

        byte[] aad = BuildAad(frame, frame.Payload.Length);

        byte[] ciphertext = new byte[frame.Payload.Length];
        byte[] tag = new byte[16];

        using var aead = new ChaCha20Poly1305(key32);
        aead.Encrypt(nonce, frame.Payload, ciphertext, tag, aad);

        byte[] securePayload = new byte[ciphertext.Length + tag.Length];
        Buffer.BlockCopy(ciphertext, 0, securePayload, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, securePayload, ciphertext.Length, tag.Length);

        return Encode(new BinaryFrame(
            PayloadLength: new UInt16HbLb((ushort)securePayload.Length),
            Type: frame.Type,
            Flags: frame.Flags,
            Seq: frame.Seq,
            Payload: securePayload,
            Crc16: new UInt16HbLb(0)
        ));
    }

    private static byte[] BuildNonce(byte[] baseNonce, uint seq)
    {
        byte[] nonce = (byte[])baseNonce.Clone();

        nonce[8] = (byte)(seq & 0xFF);
        nonce[9] = (byte)((seq >> 8) & 0xFF);
        nonce[10] = (byte)((seq >> 16) & 0xFF);
        nonce[11] = (byte)((seq >> 24) & 0xFF);

        return nonce;
    }

    private byte[] BuildAad(BinaryFrame frame, int payloadLen)
    {
        const int TagSize = 16;

        ushort aeadLenAfterLen = checked((ushort)(
            2 + 1 + 4 + payloadLen + TagSize + 1));

        byte[] aad = new byte[9];
        int a = 0;

        aad[a++] = (byte)(aeadLenAfterLen & 0xFF);
        aad[a++] = (byte)(aeadLenAfterLen >> 8);
        aad[a++] = (byte)(frame.Type.Value & 0xFF);
        aad[a++] = (byte)(frame.Type.Value >> 8);
        aad[a++] = frame.Flags;
        aad[a++] = (byte)(frame.Seq & 0xFF);
        aad[a++] = (byte)(frame.Seq >> 8);
        aad[a++] = (byte)(frame.Seq >> 16);
        aad[a++] = (byte)(frame.Seq >> 24);

        return aad;
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
        WriteU16LE(header, h, frame.Type.Value); h += TypeSize;
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