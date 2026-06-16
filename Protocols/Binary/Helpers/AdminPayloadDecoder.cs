using ProtocolWorkbench.Core.Enums;
using ProtocolWorkbench.Core.Models.ApiResponses;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class AdminPayloadDecoder
    {
        public static ChainStatusResponse DecodeGetChainStatus(ReadOnlySpan<byte> payload)
        {
            var reader = new PayloadReader(payload);
            var status = reader.ReadStatus();

            if (status != RpcStatus.Ok)
                return new ChainStatusResponse(status, null, null, null, null);

            var chainName = reader.ReadString();
            var isReady = reader.ReadBool();
            var activeIntermediate = reader.ReadNullableChainStatusIntermediate();
            var activePolicy = reader.ReadNullableTrustPolicySummary();
            reader.IgnoreOptionalPadding();

            return new ChainStatusResponse(status, chainName, isReady, activeIntermediate, activePolicy);
        }

        public static IntermediateCertificateResponse DecodeImportIntermediate(ReadOnlySpan<byte> payload)
        {
            var reader = new PayloadReader(payload);
            var status = reader.ReadStatus();

            if (status != RpcStatus.Ok)
                return new IntermediateCertificateResponse(status, null, null, null, null, null, null, null, null, null);

            var result = reader.ReadImportIntermediate(status);
            reader.IgnoreOptionalPadding();
            return result;
        }

        public static TrustPolicySummaryResponse DecodeGenerateTrustPolicy(ReadOnlySpan<byte> payload)
        {
            var reader = new PayloadReader(payload);
            var status = reader.ReadStatus();

            if (status != RpcStatus.Ok)
                return new TrustPolicySummaryResponse(status, null, null, null, null, null, null);

            var result = reader.ReadTrustPolicySummary(status);
            reader.IgnoreOptionalPadding();
            return result;
        }

        private sealed class PayloadReader
        {
            private readonly ReadOnlyMemory<byte> _payload;
            private int _offset;

            public PayloadReader(ReadOnlySpan<byte> payload)
            {
                _payload = payload.ToArray();
                _offset = 0;
            }

            public RpcStatus ReadStatus()
            {
                EnsureAvailable(1, "status");
                return (RpcStatus)_payload.Span[_offset++];
            }

            public bool ReadBool()
            {
                EnsureAvailable(1, "bool");
                return _payload.Span[_offset++] != 0;
            }

            public ushort ReadU16()
            {
                EnsureAvailable(2, "u16");
                ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_payload.Span.Slice(_offset, 2));
                _offset += 2;
                return value;
            }

            public uint ReadU32()
            {
                EnsureAvailable(4, "u32");
                uint value = BinaryPrimitives.ReadUInt32LittleEndian(_payload.Span.Slice(_offset, 4));
                _offset += 4;
                return value;
            }

            public string ReadString()
            {
                ushort len = ReadU16();
                if (len == 0)
                    return string.Empty;

                EnsureAvailable(len, "string");
                string value = Encoding.UTF8.GetString(_payload.Span.Slice(_offset, len));
                _offset += len;
                return value;
            }

            public byte[] ReadBytes()
            {
                ushort len = ReadU16();
                if (len == 0)
                    return Array.Empty<byte>();

                EnsureAvailable(len, "byte[]");
                byte[] value = _payload.Span.Slice(_offset, len).ToArray();
                _offset += len;
                return value;
            }

            public DateTime ReadUtcDateTime()
            {
                var text = ReadString();
                return DateTime.Parse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }

            public Guid ReadGuid()
            {
                var text = ReadString();
                return Guid.Parse(text);
            }

            public ChainStatusIntermediateResponse? ReadNullableChainStatusIntermediate()
            {
                if (!TryReadPresence(out bool present))
                    return null;

                return present ? ReadChainStatusIntermediate(RpcStatus.Ok) : null;
            }

            public TrustPolicySummaryResponse? ReadNullableTrustPolicySummary()
            {
                if (!TryReadPresence(out bool present))
                    return null;

                return present ? ReadTrustPolicySummary(RpcStatus.Ok) : null;
            }

            public ChainStatusIntermediateResponse ReadChainStatusIntermediate(RpcStatus status)
            {
                var id = ReadGuid();
                var name = ReadString();
                var thumbprintHex = ReadString();
                var notBeforeUtc = ReadNullableUtcDateTime();
                var notAfterUtc = ReadNullableUtcDateTime();
                var isActive = ReadBool();
                var version = ReadU32();

                return new ChainStatusIntermediateResponse(
                    status,
                    id,
                    name,
                    thumbprintHex,
                    notBeforeUtc,
                    notAfterUtc,
                    isActive,
                    version);
            }

            public IntermediateCertificateResponse ReadImportIntermediate(RpcStatus status)
            {
                var id = ReadGuid();
                var name = ReadString();
                var thumbprintHex = ReadString();
                var spkiHash = ReadBytes();
                var createdUtc = ReadUtcDateTime();
                var notBeforeUtc = ReadNullableUtcDateTime();
                var notAfterUtc = ReadNullableUtcDateTime();
                var isActive = ReadBool();
                var version = ReadU32();

                return new IntermediateCertificateResponse(
                    status,
                    id,
                    name,
                    thumbprintHex,
                    spkiHash,
                    createdUtc,
                    notBeforeUtc,
                    notAfterUtc,
                    isActive,
                    version);
            }

            public TrustPolicySummaryResponse ReadTrustPolicySummary(RpcStatus status)
            {
                var id = ReadGuid();
                var version = ReadU32();
                var intermediateCertificateId = ReadGuid();
                var createdUtc = ReadUtcDateTime();
                var publishedUtc = ReadNullableUtcDateTime();
                var isActive = ReadBool();

                return new TrustPolicySummaryResponse(
                    status,
                    id,
                    version,
                    intermediateCertificateId,
                    createdUtc,
                    publishedUtc,
                    isActive);
            }

            public DateTime? ReadNullableUtcDateTime()
            {
                if (!TryReadPresence(out bool present))
                    return null;

                return present ? ReadUtcDateTime() : null;
            }

            public void IgnoreOptionalPadding()
            {
                while (_offset < _payload.Length && _payload.Span[_offset] == 0)
                    _offset++;

                if (_offset < _payload.Length)
                {
                    throw new InvalidOperationException(
                        $"Unexpected trailing bytes at offset {_offset} of {_payload.Length}: {BitConverter.ToString(_payload.Span.Slice(_offset).ToArray())}");
                }
            }

            private bool TryReadPresence(out bool present)
            {
                if (_offset >= _payload.Length)
                {
                    present = false;
                    return false;
                }

                byte marker = _payload.Span[_offset++];
                present = marker != 0;
                return true;
            }

            private void EnsureAvailable(int count, string fieldName)
            {
                if (_offset + count > _payload.Length)
                {
                    throw new InvalidOperationException(
                        $"Payload too short while reading '{fieldName}' at offset {_offset}; need {count}, remaining {_payload.Length - _offset}.");
                }
            }
        }
    }
}
