using ProtocolWorkbench.Core.Enums;
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

    public byte[] EncodeSecureAes128Ccm(BinaryFrame frame, byte[] key16, byte[] nonceBase13)
    {
        const int TagSize = 16;

        if (frame.Payload is null)
            throw new ArgumentNullException(nameof(frame.Payload));

        if (key16.Length != 16)
            throw new ArgumentException("Key must be 16 bytes.", nameof(key16));

        if (nonceBase13.Length != 13)
            throw new ArgumentException("Nonce base must be 13 bytes.", nameof(nonceBase13));

        byte[] nonce = BuildNonce(nonceBase13, frame.Seq);
        byte[] aad = BuildAad(frame, frame.Payload.Length);

        byte[] ciphertext = new byte[frame.Payload.Length];
        byte[] tag = new byte[TagSize];

        using var aead = new AesCcm(key16);
        aead.Encrypt(nonce, frame.Payload, ciphertext, tag, aad);

        ushort lenAfterLen = checked((ushort)(
            TypeSize + FlagsSize + SeqSize + ciphertext.Length + tag.Length + 1)); // EOF only, no CRC

        byte[] buffer = new byte[1 + LenFieldSize + TypeSize + FlagsSize + SeqSize +
                                 ciphertext.Length + tag.Length + 1];

        int o = 0;
        buffer[o++] = SOF;

        WriteU16LE(buffer, o, lenAfterLen); o += LenFieldSize;
        WriteU16LE(buffer, o, frame.Type.Value); o += TypeSize;
        buffer[o++] = frame.Flags;
        WriteU32LE(buffer, o, frame.Seq); o += SeqSize;

        ciphertext.CopyTo(buffer.AsSpan(o)); o += ciphertext.Length;
        tag.CopyTo(buffer.AsSpan(o)); o += tag.Length;

        buffer[o++] = EOF;

        return buffer;
    }

    private static byte[] BuildNonce(byte[] baseNonce, uint seq)
    {
        byte[] nonce = (byte[])baseNonce.Clone();

        nonce[9] = (byte)(seq & 0xFF);
        nonce[10] = (byte)((seq >> 8) & 0xFF);
        nonce[11] = (byte)((seq >> 16) & 0xFF);
        nonce[12] = (byte)((seq >> 24) & 0xFF);

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

    public List<Byte> ParameterToBytesLSBFirst(MessageParameter param)
    {
        List<Byte> formattedPayload = new List<byte>();

        if (param.CType == CTypes.BOOL)
        {
            if (param.Value.ToLower() == "true")
            {
                formattedPayload.Add(0x01);
            }
            else
            {
                formattedPayload.Add(0x00);
            }

        }
        else if (param.CType == CTypes.BASE64)
        {
            if (string.IsNullOrWhiteSpace(param.Value))
                return formattedPayload;

            var bytes = Convert.FromBase64String(param.Value.Trim());
            formattedPayload.AddRange(bytes);

            // DO NOT reverse — blobs are not little-endian numbers
        }
        else if (param.CType == CTypes.UINT8)
        {
            formattedPayload.Add(Convert.ToByte(param.Value));
        }
        else if (param.CType == CTypes.UINT16)
        {
            UInt16 u16 = Convert.ToUInt16(param.Value);
            formattedPayload.Add((byte)u16);
            formattedPayload.Add((byte)(u16 >> 8));
        }
        else if (param.CType == CTypes.UINT32)
        {
            UInt32 u32 = Convert.ToUInt32(param.Value);
            formattedPayload.Add((byte)u32);
            formattedPayload.Add((byte)(u32 >> 8));
            formattedPayload.Add((byte)(u32 >> 16));
            formattedPayload.Add((byte)(u32 >> 24));
        }
        else if (param.CType == CTypes.UINT64)
        {
            UInt64 u64 = Convert.ToUInt64(param.Value);
            formattedPayload.Add((byte)u64);
            formattedPayload.Add((byte)(u64 >> 8));
            formattedPayload.Add((byte)(u64 >> 16));
            formattedPayload.Add((byte)(u64 >> 24));
            formattedPayload.Add((byte)(u64 >> 32));
            formattedPayload.Add((byte)(u64 >> 40));
            formattedPayload.Add((byte)(u64 >> 48));
            formattedPayload.Add((byte)(u64 >> 56));
        }
        else if (param.CType == CTypes.INT8)
        {
            formattedPayload.Add(Convert.ToByte(param.Value));
        }
        else if (param.CType == CTypes.INT16)
        {
            Int16 i16 = Convert.ToInt16(param.Value);
            formattedPayload.Add((byte)i16);
            formattedPayload.Add((byte)(i16 >> 8));
        }
        else if (param.CType == CTypes.INT32)
        {
            Int32 i32 = Convert.ToInt32(param.Value);
            formattedPayload.Add((byte)i32);
            formattedPayload.Add((byte)(i32 >> 8));
            formattedPayload.Add((byte)(i32 >> 16));
            formattedPayload.Add((byte)(i32 >> 24));
        }
        else if (param.CType == CTypes.INT64)
        {
            Int64 i64 = Convert.ToInt64(param.Value);
            formattedPayload.Add((byte)i64);
            formattedPayload.Add((byte)(i64 >> 8));
            formattedPayload.Add((byte)(i64 >> 16));
            formattedPayload.Add((byte)(i64 >> 24));
            formattedPayload.Add((byte)(i64 >> 32));
            formattedPayload.Add((byte)(i64 >> 40));
            formattedPayload.Add((byte)(i64 >> 48));
            formattedPayload.Add((byte)(i64 >> 56));
        }
        else if (param.CType == CTypes.STRING)
        {
            if (string.IsNullOrEmpty(param.Value))
                return formattedPayload;

            formattedPayload.AddRange(System.Text.Encoding.UTF8.GetBytes(param.Value));
        }
        else if (param.CType == CTypes.BYTE_ARRAY)
        {
            string[] strArray = null;
            if (param.Value.Contains(','))
            {
                strArray = param.Value.Split(',');
            }
            else if (param.Value.Contains(' '))
            {
                strArray = param.Value.Split(' ');
            }
            else
            {
                byte b = 0;
                b = Convert.ToByte(param.Value);
                formattedPayload.Add(b);
            }
            if (null != strArray)
            {
                foreach (string s in strArray)
                {
                    byte b = 0;
                    string strTrim = s.Trim();
                    if ((strTrim.StartsWith("0x") || (strTrim.StartsWith("Ox"))))
                    {
                        /* convert hex string to byte */
                        string num = strTrim.Substring(2, strTrim.Length - 2);
                        int intNum = Int32.Parse(num, System.Globalization.NumberStyles.HexNumber);
                        b = (byte)intNum;
                    }
                    else
                    {
                        b = Convert.ToByte(s);
                    }
                    formattedPayload.Add(b);
                }
                /* send lsb first */
                formattedPayload.Reverse();
            }
        }

        return formattedPayload;
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