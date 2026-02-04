using ProtocolWorkbench.Core.Enums;
using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.Binary.Models;
using System.Runtime.InteropServices;

namespace ProtocolWorkbench.Core.Protocols.Binary.Client;

public sealed class BinaryMessageBuilder : IBinaryMessageBuilder
{
    public const byte SOF = 0xAA;
    public const byte EOF = 0x55;

    private readonly ICrcService _crc;

    public BinaryMessageBuilder(ICrcService crc)
    {
        _crc = crc ?? throw new ArgumentNullException(nameof(crc));
    }

    public List<byte> CreateBinaryMessage(UInt16HbLb id, IReadOnlyList<MessageParameter> msgParams)
    {
        if (msgParams is null) throw new ArgumentNullException(nameof(msgParams));

        var binary = new BinaryProtocol
        {
            StartOfFrame = SOF,
            EndOfFrame = EOF,
            MesssageType = id,
        };

        foreach (var msgParam in msgParams)
            binary.Payload.AddRange(ParameterToBytesLSBFirst(msgParam));

        // Your existing length logic (payload count + 7) looks like:
        // TYPE(2) + PAYLOAD + CRC(2) + EOF(1) + ??? = 7
        // I’m not changing it here—just keeping behavior identical.
        var length = new UInt16HbLb { U16Value = (ushort)(binary.Payload.Count + 7) };
        binary.Length.Lb = length.Lb;
        binary.Length.Hb = length.Hb;

        // Build message with placeholder CRC (current code computes CRC over message bytes minus last 3)
        var msgWithoutFinalCrc = BinaryProtocolToListByte(binary);

        var span = CollectionsMarshal.AsSpan(msgWithoutFinalCrc);
        var crc = _crc.ComputeCcitt16(span[..^3]);

        binary.CRC.Lb = crc.Lb;
        binary.CRC.Hb = crc.Hb;

        return BinaryProtocolToListByte(binary);
    }

    private static List<byte> BinaryProtocolToListByte(BinaryProtocol binaryProtocol)
    {
        var value = new List<byte>(9 + binaryProtocol.Payload.Count);

        value.Add(binaryProtocol.StartOfFrame);
        value.Add(binaryProtocol.Length.Lb);
        value.Add(binaryProtocol.Length.Hb);
        value.Add(binaryProtocol.MesssageType.Lb);
        value.Add(binaryProtocol.MesssageType.Hb);
        value.AddRange(binaryProtocol.Payload);
        value.Add(binaryProtocol.CRC.Lb);
        value.Add(binaryProtocol.CRC.Hb);
        value.Add(binaryProtocol.EndOfFrame);

        return value;
    }

    // Kept as-is for now (behavior parity). We can refactor this next.
    private static List<byte> ParameterToBytesLSBFirst(MessageParameter param)
    {
        if (param is null) throw new ArgumentNullException(nameof(param));

        var formattedPayload = new List<byte>();

        if (param.CType == CTypes.BOOL)
        {
            formattedPayload.Add(param.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ? (byte)0x01 : (byte)0x00);
        }
        else if (param.CType == CTypes.UINT8)
        {
            formattedPayload.Add(Convert.ToByte(param.Value));
        }
        else if (param.CType == CTypes.UINT16)
        {
            ushort u16 = Convert.ToUInt16(param.Value);
            formattedPayload.Add((byte)u16);
            formattedPayload.Add((byte)(u16 >> 8));
        }
        else if (param.CType == CTypes.UINT32)
        {
            uint u32 = Convert.ToUInt32(param.Value);
            formattedPayload.Add((byte)u32);
            formattedPayload.Add((byte)(u32 >> 8));
            formattedPayload.Add((byte)(u32 >> 16));
            formattedPayload.Add((byte)(u32 >> 24));
        }
        else if (param.CType == CTypes.UINT64)
        {
            ulong u64 = Convert.ToUInt64(param.Value);
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
            short i16 = Convert.ToInt16(param.Value);
            formattedPayload.Add((byte)i16);
            formattedPayload.Add((byte)(i16 >> 8));
        }
        else if (param.CType == CTypes.INT32)
        {
            int i32 = Convert.ToInt32(param.Value);
            formattedPayload.Add((byte)i32);
            formattedPayload.Add((byte)(i32 >> 8));
            formattedPayload.Add((byte)(i32 >> 16));
            formattedPayload.Add((byte)(i32 >> 24));
        }
        else if (param.CType == CTypes.INT64)
        {
            long i64 = Convert.ToInt64(param.Value);
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
            // NOTE: this format is weird (string of comma-separated bytes), but preserving behavior.
            string[] strArray = param.Value.Split(',');
            foreach (string s in strArray)
                formattedPayload.Add(Convert.ToByte(s));

            formattedPayload.Reverse();
        }
        else if (param.CType == CTypes.BYTE_ARRAY)
        {
            string[]? strArray = null;

            if (param.Value.Contains(','))
                strArray = param.Value.Split(',');
            else if (param.Value.Contains(' '))
                strArray = param.Value.Split(' ');
            else
                formattedPayload.Add(Convert.ToByte(param.Value));

            if (strArray is not null)
            {
                foreach (var s in strArray)
                {
                    var strTrim = s.Trim();

                    byte b;
                    if (strTrim.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        var num = strTrim.Substring(2);
                        b = (byte)int.Parse(num, System.Globalization.NumberStyles.HexNumber);
                    }
                    else
                    {
                        b = Convert.ToByte(strTrim);
                    }

                    formattedPayload.Add(b);
                }

                formattedPayload.Reverse();
            }
        }

        return formattedPayload;
    }
}