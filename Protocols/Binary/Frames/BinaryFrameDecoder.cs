using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using System.Buffers;
using System.Diagnostics;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames;

public sealed class BinaryFrameDecoder : IBinaryFrameDecoder
{
    public const byte SOF = 0xAA;
    public const byte EOF = 0x55;

    private const byte FlagSecure = 1 << 7;

    private const int LenSize = 2;   // u16
    private const int TypeSize = 2;  // u16
    private const int FlagsSize = 1; // u8
    private const int SeqSize = 4;   // u32
    private const int CrcSize = 2;   // u16

    // Header on wire after SOF: LEN + TYPE + FLAGS + SEQ
    private const int HeaderSize = LenSize + TypeSize + FlagsSize + SeqSize;

    // Plaintext:
    //   LEN covers TYPE + FLAGS + SEQ + PAYLOAD + CRC + EOF
    private const int FixedAfterLenNoPayloadPlain =
        TypeSize + FlagsSize + SeqSize + CrcSize + 1;

    // Secure:
    //   LEN covers TYPE + FLAGS + SEQ + CIPHERTEXT_AND_TAG + EOF
    //   No CRC. AEAD tag authenticates the header and payload.
    private const int FixedAfterLenNoPayloadSecure =
        TypeSize + FlagsSize + SeqSize + 1;

    private readonly int _maxPayloadLength;
    private readonly ICrcService _crc;

    public event Action<BinaryFrame>? FrameDecoded;
    public event Action<string>? FrameError;

    private readonly Action<string>? _trace;
    public bool TraceEnabled { get; set; } = true;

    private enum State
    {
        SeekingSof,
        ReadingHeader,
        ReadingPayload,
        ReadingCrc,
        ReadingEof
    }

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

    private bool IsSecureFrame => (_flags & FlagSecure) != 0;

    public BinaryFrameDecoder(ICrcService crc, int maxPayloadLength = 4096, Action<string>? trace = null)
    {
        _crc = crc ?? throw new ArgumentNullException(nameof(crc));
        _maxPayloadLength = Math.Max(0, maxPayloadLength);
        _trace = trace;
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
                    Trace("SOF found -> ReadingHeader");
                    _state = State.ReadingHeader;
                    _headerIndex = 0;
                }
                return;

            case State.ReadingHeader:
                _header[_headerIndex++] = b;

                if (_headerIndex == HeaderSize)
                {
                    _lenAfterLen = ReadU16LE(_header, 0);
                    _type = ReadU16LE(_header, 2);
                    _flags = _header[4];
                    _seq = ReadU32LE(_header, 5);

                    Trace($"Header done: LEN(afterLEN)={_lenAfterLen} TYPE=0x{_type:X4} FLAGS=0x{_flags:X2} SEQ={_seq} secure={IsSecureFrame}");

                    int fixedAfterLenNoPayload = IsSecureFrame
                        ? FixedAfterLenNoPayloadSecure
                        : FixedAfterLenNoPayloadPlain;

                    if (_lenAfterLen < fixedAfterLenNoPayload)
                    {
                        EmitError($"LEN {_lenAfterLen} too small (min {fixedAfterLenNoPayload}). Resync.");
                        Trace("LEN too small -> Reset()");
                        Reset();
                        return;
                    }

                    _payloadLen = _lenAfterLen - fixedAfterLenNoPayload;
                    Trace($"Computed payloadLen={_payloadLen} secure={IsSecureFrame} fixed={fixedAfterLenNoPayload}");

                    if (_payloadLen > _maxPayloadLength)
                    {
                        EmitError($"Payload {_payloadLen} exceeds max {_maxPayloadLength}. Resync.");
                        Trace("Payload too large -> Reset()");
                        Reset();
                        return;
                    }

                    _payload = _payloadLen == 0 ? Array.Empty<byte>() : new byte[_payloadLen];
                    _payloadIndex = 0;

                    _state = _payloadLen == 0
                        ? (IsSecureFrame ? State.ReadingEof : State.ReadingCrc)
                        : State.ReadingPayload;

                    if (_state == State.ReadingCrc)
                    {
                        _crcIndex = 0;
                    }

                    Trace($"Transition -> {_state}");
                }
                return;

            case State.ReadingPayload:
                _payload![_payloadIndex++] = b;

                if (_payloadIndex == _payloadLen)
                {
                    if (IsSecureFrame)
                    {
                        Trace("Secure payload complete -> ReadingEof");
                        _state = State.ReadingEof;
                    }
                    else
                    {
                        Trace("Payload complete -> ReadingCrc");
                        _state = State.ReadingCrc;
                        _crcIndex = 0;
                    }
                }
                return;

            case State.ReadingCrc:
                _crcBytes[_crcIndex++] = b;

                if (_crcIndex == CrcSize)
                {
                    _rxCrc = ReadU16LE(_crcBytes, 0);
                    Trace($"CRC bytes read: rxCrc=0x{_rxCrc:X4} -> ReadingEof");
                    _state = State.ReadingEof;
                }
                return;

            case State.ReadingEof:
                if (b != EOF)
                {
                    EmitError($"Missing EOF (got 0x{b:X2}). Resync.");
                    Trace($"EOF missing (got 0x{b:X2}) -> Reset()");
                    Reset();
                    return;
                }

                if (!IsSecureFrame)
                {
                    Trace("EOF OK. Computing CRC...");

                    var computed = ComputeCrc_LenThroughPayload();
                    Trace($"CRC compare: rx=0x{_rxCrc:X4}, calc=0x{computed.U16Value:X4}");

                    if (computed.U16Value != _rxCrc)
                    {
                        EmitError($"CRC mismatch. rx=0x{_rxCrc:X4}, calc=0x{computed.U16Value:X4}. Resync.");
                        Trace("CRC mismatch -> Reset()");
                        Reset();
                        return;
                    }
                }
                else
                {
                    Trace("Secure EOF OK. Skipping CRC.");
                }

                Trace($"FrameDecoded TYPE=0x{_type:X4} FLAGS=0x{_flags:X2} SEQ={_seq} payloadLen={_payloadLen} secure={IsSecureFrame}");

                var frame = new BinaryFrame(
                    PayloadLength: new UInt16HbLb((ushort)_payloadLen),
                    Type: new Models.MessageType(_type),
                    Flags: _flags,
                    Seq: _seq,
                    Payload: _payload ?? Array.Empty<byte>(),
                    Crc16: new UInt16HbLb(_rxCrc)
                );

                FrameDecoded?.Invoke(frame);
                Reset();
                return;

            default:
                Reset();
                return;
        }
    }

    private UInt16HbLb ComputeCrc_LenThroughPayload()
    {
        // CRC covers:
        // LEN(2) + TYPE(2) + FLAGS(1) + SEQ(4) + PAYLOAD(N)
        // Excludes SOF, CRC, EOF.
        int totalLen = HeaderSize + _payloadLen;

        byte[] rented = ArrayPool<byte>.Shared.Rent(totalLen);
        try
        {
            Buffer.BlockCopy(_header, 0, rented, 0, HeaderSize);

            if (_payloadLen > 0 && _payload is not null)
            {
                Buffer.BlockCopy(_payload, 0, rented, HeaderSize, _payloadLen);
            }

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

    private void Trace(string msg)
    {
#if DEBUG
        if (!TraceEnabled) return;

        var line = $"[Decoder] {msg}";
        if (_trace is not null) _trace(line);
        else Debug.WriteLine(line);
#endif
    }
}
