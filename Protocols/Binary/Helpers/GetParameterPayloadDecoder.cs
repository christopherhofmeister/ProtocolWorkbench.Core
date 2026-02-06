using ProtocolWorkbench.Core.Enums;
using ProtocolWorkbench.Core.Protocols.Binary.Models;
using System.Buffers.Binary;
using System.Text;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class GetParameterPayloadDecoder
    {
        public static GetParameterResponse Decode(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 2)
                throw new InvalidOperationException($"GetParameter payload too short: {payload.Length}");

            var status = (RpcStatus)payload[0];
            var paramId = payload[1];

            // Error response: only status + paramId
            if (status != RpcStatus.Ok)
            {
                return new GetParameterResponse(status, paramId, null, null, null);
            }

            if (payload.Length < 2 + 1 + 4)
                throw new InvalidOperationException($"GetParameter OK payload too short: {payload.Length}");

            var valueType = (CTypes)payload[2];

            uint valueLen = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(3, 4));

            // Guardrails
            var remaining = payload.Length - (2 + 1 + 4);
            if (valueLen > (uint)remaining)
                throw new InvalidOperationException($"ValueLen={valueLen} exceeds remaining={remaining}");

            var valueBytes = payload.Slice(7, (int)valueLen);

            object? value = DecodeByType(valueType, valueBytes);

            return new GetParameterResponse(status, paramId, valueType, valueLen, value);
        }

        private static object? DecodeByType(CTypes type, ReadOnlySpan<byte> bytes)
        {
            switch (type)
            {
                case CTypes.BOOL:
                    if (bytes.Length < 1) return false;
                    return bytes[0] != 0;

                case CTypes.UINT8:
                    if (bytes.Length < 1) return (byte)0;
                    return bytes[0];

                case CTypes.INT8:
                    if (bytes.Length < 1) return (sbyte)0;
                    return unchecked((sbyte)bytes[0]);

                case CTypes.UINT16:
                    Require(bytes, 2, type);
                    return BinaryPrimitives.ReadUInt16LittleEndian(bytes);

                case CTypes.INT16:
                    Require(bytes, 2, type);
                    return BinaryPrimitives.ReadInt16LittleEndian(bytes);

                case CTypes.UINT32:
                    Require(bytes, 4, type);
                    return BinaryPrimitives.ReadUInt32LittleEndian(bytes);

                case CTypes.INT32:
                    Require(bytes, 4, type);
                    return BinaryPrimitives.ReadInt32LittleEndian(bytes);

                case CTypes.UINT64:
                    Require(bytes, 8, type);
                    return BinaryPrimitives.ReadUInt64LittleEndian(bytes);

                case CTypes.INT64:
                    Require(bytes, 8, type);
                    return BinaryPrimitives.ReadInt64LittleEndian(bytes);

                case CTypes.FLOAT:
                    Require(bytes, 4, type);
                    return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));

                case CTypes.DOUBLE:
                    Require(bytes, 8, type);
                    return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes));

                case CTypes.STRING:
                    // Assuming UTF-8, no null terminator required (ValueLen defines size)
                    return Encoding.UTF8.GetString(bytes);

                case CTypes.BYTE_ARRAY:
                    return bytes.ToArray();

                default:
                    // Unknown enum value
                    return bytes.ToArray();
            }
        }

        private static void Require(ReadOnlySpan<byte> bytes, int needed, CTypes t)
        {
            if (bytes.Length < needed)
                throw new InvalidOperationException($"Not enough bytes for {t}: need {needed}, got {bytes.Length}");
        }
    }
}
