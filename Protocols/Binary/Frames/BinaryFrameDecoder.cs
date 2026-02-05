using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using System.Buffers;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames;

public sealed class BinaryFrameDecoder : IBinaryFrameDecoder
{
    public const byte SOF = 0xAA;
    public const byte EOF = 0x55;

    private const int LenSize = 2; // u16
    private const int TypeSize = 2; // u16
    private const int FlagsSize = 1; // u8
    private const int SeqSize = 4; // u32
    private const int CrcSize = 2; // u16

    // Header on wire after SOF: LEN + TYPE + FLAGS + SEQ
    private const int HeaderSize = LenSize + TypeSize + FlagsSize + SeqSize;

    // Bytes after LEN that are always present, excluding payload:
    // TYPE + FLAGS + SEQ + CRC + EOF
    private const int FixedAfterLenNoPayload =
        TypeSize + FlagsSize + SeqSize + CrcSize + 1; // EOF

    private readonly int _maxPayloadLength;
    private readonly ICrcService _crc;

    public event Action<BinaryFrame>? FrameDecoded;
    public event Action<string>? FrameError;

    private enum State { SeekingSof, ReadingHeader, ReadingPayload, ReadingCrc, ReadingEof }
    private State _state = State.SeekingSof;

    private readonly byte[] _header = new byte[HeaderSize];
    private int _headerIndex;

    private ushort _lenAfterLen;
    private ushort _type;
    private byte _flags;
    private uint _seq;

    private byte[]? _payload;
    private int _payloadIndex;
    private int _payloadLen;

    private readonly byte[] _crcBytes = new byte[CrcSize];
    private int _crcIndex;
    private ushort _rxCrc;

    public BinaryFrameDecoder(ICrcService crc, int maxPayloadLength = 4096)
    {
        _crc = crc ?? throw new ArgumentNullException(nameof(crc));
        _maxPayloadLength = Math.Max(0, maxPayloadLength);
    }

    public void Reset()
    {
        _state = State.SeekingSof;

        _headerIndex = 0;

        _lenAfterLen = 0;
        _type = 0;
        _flags = 0;
        _seq = 0;

        _payload = null;
        _payloadIndex = 0;
        _payloadLen = 0;

        _crcIndex = 0;
        _rxCrc = 0;
    }

    public void PushByte(byte b)
    {
        switch (_state)
        {
            case State.SeekingSof:
                if (b == SOF)
                {
                    _state = State.ReadingHeader;
                    _headerIndex = 0;
                }
                return;

            case State.ReadingHeader:
                _header[_headerIndex++] = b;
                if (_headerIndex == HeaderSize)
                {
                    // Header layout: LEN(2) TYPE(2) FLAGS(1) SEQ(4)
                    _lenAfterLen = ReadU16LE(_header, 0);
                    _type = ReadU16LE(_header, 2);
                    _flags = _header[4];
                    _seq = ReadU32LE(_header, 5);

                    // LEN is bytes AFTER LEN field:
                    // TYPE + FLAGS + SEQ + PAYLOAD + CRC + EOF
                    if (_lenAfterLen < FixedAfterLenNoPayload)
                    {
                        EmitError($"LEN {_lenAfterLen} too small (min {FixedAfterLenNoPayload}). Resync.");
                        Reset();
                        return;
                    }

                    _payloadLen = _lenAfterLen - FixedAfterLenNoPayload;

                    if (_payloadLen > _maxPayloadLength)
                    {
                        EmitError($"Payload {_payloadLen} exceeds max {_maxPayloadLength}. Resync.");
                        Reset();
                        return;
                    }

                    _payload = _payloadLen == 0 ? Array.Empty<byte>() : new byte[_payloadLen];
                    _payloadIndex = 0;

                    _state = _payloadLen == 0 ? State.ReadingCrc : State.ReadingPayload;
                }
                return;

            case State.ReadingPayload:
                _payload![_payloadIndex++] = b;
                if (_payloadIndex == _payloadLen)
                {
                    _state = State.ReadingCrc;
                    _crcIndex = 0;
                }
                return;

            case State.ReadingCrc:
                _crcBytes[_crcIndex++] = b;
                if (_crcIndex == CrcSize)
                {
                    _rxCrc = ReadU16LE(_crcBytes, 0);
                    _state = State.ReadingEof;
                }
                return;

            case State.ReadingEof:
                if (b != EOF)
                {
                    EmitError($"Missing EOF (got 0x{b:X2}). Resync.");
                    Reset();
                    return;
                }

                // CRC must match encoder. Your encoder does CRC over:
                // TYPE + FLAGS + SEQ + PAYLOAD
                var computed = ComputeCrc_TypeThroughPayload();
                if (computed.U16Value != _rxCrc)
                {
                    EmitError($"CRC mismatch. rx=0x{_rxCrc:X4}, calc=0x{computed.U16Value:X4}. Resync.");
                    Reset();
                    return;
                }

                var frame = new BinaryFrame(
                    PayloadLength: new UInt16HbLb((ushort)_payloadLen),
                    Type: new UInt16HbLb(_type),
                    Flags: _flags,
                    Seq: _seq,
                    Payload: _payload ?? Array.Empty<byte>(),
                    Crc16: new UInt16HbLb(_rxCrc)
                );

                FrameDecoded?.Invoke(frame);
                Reset();
                return;
        }
    }

    private UInt16HbLb ComputeCrc_TypeThroughPayload()
    {
        // Build bytes: TYPE(2) + FLAGS(1) + SEQ(4) + PAYLOAD(N)
        int typeToSeqLen = TypeSize + FlagsSize + SeqSize;
        int totalLen = typeToSeqLen + _payloadLen;

        byte[] rented = ArrayPool<byte>.Shared.Rent(totalLen);
        try
        {
            int o = 0;

            // TYPE (from header offset 2)
            rented[o++] = _header[2];
            rented[o++] = _header[3];

            // FLAGS (header[4])
            rented[o++] = _header[4];

            // SEQ (header offset 5..8)
            rented[o++] = _header[5];
            rented[o++] = _header[6];
            rented[o++] = _header[7];
            rented[o++] = _header[8];

            // PAYLOAD
            if (_payloadLen > 0 && _payload is not null)
                Buffer.BlockCopy(_payload, 0, rented, o, _payloadLen);

            return _crc.ComputeCcitt16(rented.AsSpan(0, totalLen));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static ushort ReadU16LE(byte[] buf, int offset)
        => (ushort)(buf[offset] | (buf[offset + 1] << 8));

    private static uint ReadU32LE(byte[] buf, int offset)
        => (uint)(buf[offset]
               | (buf[offset + 1] << 8)
               | (buf[offset + 2] << 16)
               | (buf[offset + 3] << 24));

    private void EmitError(string msg) => FrameError?.Invoke(msg);
}