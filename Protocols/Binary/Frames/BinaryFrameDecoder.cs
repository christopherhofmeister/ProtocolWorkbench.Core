using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using System.Buffers;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames;

public sealed class BinaryFrameDecoder : IBinaryFrameDecoder
{
    public const byte SOF = 0xAA;
    public const byte EOF = 0x55;

    // sanity limit to avoid runaway allocations if LEN is corrupt
    private readonly int _maxPayloadLength;

    private readonly CrcService _crc;

    public event Action<BinaryFrame>? FrameDecoded;
    public event Action<string>? FrameError;

    // State
    private enum State { SeekingSof, ReadingHeader, ReadingPayload, ReadingCrc, ReadingEof }
    private State _state = State.SeekingSof;

    // LEN(2)+TYPE(2)+FLAGS(1)+SEQ(4)
    private readonly byte[] _header = new byte[2 + 2 + 1 + 4];
    private int _headerIndex;

    private UInt16HbLb _len = new(0);
    private UInt16HbLb _type = new(0);
    private byte _flags;
    private uint _seq;

    private byte[]? _payload;
    private int _payloadIndex;

    private UInt16HbLb _rxCrc = new(0);
    private int _crcIndex;
    private readonly byte[] _crcBytes = new byte[2];

    public BinaryFrameDecoder(CrcService crc, int maxPayloadLength = 4096)
    {
        _crc = crc ?? throw new ArgumentNullException(nameof(crc));
        _maxPayloadLength = Math.Max(0, maxPayloadLength);
    }

    public void Reset()
    {
        _state = State.SeekingSof;
        _headerIndex = 0;

        _len = new UInt16HbLb(0);
        _type = new UInt16HbLb(0);
        _flags = 0;
        _seq = 0;

        _payload = null;
        _payloadIndex = 0;

        _rxCrc = new UInt16HbLb(0);
        _crcIndex = 0;
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
                if (_headerIndex == _header.Length)
                {
                    // Decode header fields
                    var lenU16 = ReadU16LE(_header, 0);
                    var typeU16 = ReadU16LE(_header, 2);

                    _len = new UInt16HbLb(lenU16);
                    _type = new UInt16HbLb(typeU16);
                    _flags = _header[4];
                    _seq = ReadU32LE(_header, 5);

                    if (_len.U16Value > _maxPayloadLength)
                    {
                        EmitError($"Binary frame LEN {_len.U16Value} exceeds max {_maxPayloadLength}. Resync.");
                        Reset();
                        return;
                    }

                    _payload = _len.U16Value == 0 ? Array.Empty<byte>() : new byte[_len.U16Value];
                    _payloadIndex = 0;

                    _state = _len.U16Value == 0 ? State.ReadingCrc : State.ReadingPayload;
                }
                return;

            case State.ReadingPayload:
                _payload![_payloadIndex++] = b;
                if (_payloadIndex == _len.U16Value)
                {
                    _state = State.ReadingCrc;
                    _crcIndex = 0;
                }
                return;

            case State.ReadingCrc:
                // CRC is little-endian on wire (LSB first)
                _crcBytes[_crcIndex++] = b;
                if (_crcIndex == 2)
                {
                    _rxCrc = new UInt16HbLb(ReadU16LE(_crcBytes, 0));
                    _state = State.ReadingEof;
                }
                return;

            case State.ReadingEof:
                if (b != EOF)
                {
                    EmitError($"Binary frame missing EOF (got 0x{b:X2}). Resync.");
                    Reset();
                    return;
                }

                // Validate CRC
                var computed = ComputeCrcForCurrentFrame();
                if (computed.U16Value != _rxCrc.U16Value)
                {
                    EmitError($"CRC mismatch. rx=0x{_rxCrc.U16Value:X4}, calc=0x{computed.U16Value:X4}. Resync.");
                    Reset();
                    return;
                }

                var frame = new BinaryFrame(
                    PayloadLength: _len,
                    Type: _type,
                    Flags: _flags,
                    Seq: _seq,
                    Payload: _payload ?? Array.Empty<byte>(),
                    Crc16: _rxCrc
                );

                FrameDecoded?.Invoke(frame);

                // Ready for next frame
                Reset();
                return;
        }
    }

    private UInt16HbLb ComputeCrcForCurrentFrame()
    {
        // CRC16 over bytes from LEN through end of PAYLOAD:
        // LEN(2) + TYPE(2) + FLAGS(1) + SEQ(4) + PAYLOAD(LEN)
        var headerAndPayloadLen = _header.Length + (_payload?.Length ?? 0);

        byte[] rented = ArrayPool<byte>.Shared.Rent(headerAndPayloadLen);
        try
        {
            Buffer.BlockCopy(_header, 0, rented, 0, _header.Length);

            if (_payload is { Length: > 0 })
                Buffer.BlockCopy(_payload, 0, rented, _header.Length, _payload.Length);

            return _crc.ComputeCcitt16(rented.AsSpan(0, headerAndPayloadLen));
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