using ProtocolWorkbench.Core.Protocols.Binary.Contracts;
using ProtocolWorkbench.Core.Protocols.Binary.Frames;
using ProtocolWorkbench.Core.Protocols.Binary.Transport;
using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Client;

public sealed class BinaryJobClient
{
    private readonly IFrameRouter _coordinator;
    private readonly IBinaryProtocolTransport _transport;
    private readonly IJobIdAllocator _jobIds;
    private readonly ISeqAllocator _seqs;

    private readonly HashSet<ushort> _inFlight = new();
    private readonly object _lock = new();

    public TimeSpan AckTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan JobTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public BinaryJobClient(
        IFrameRouter coordinator,
        IBinaryProtocolTransport transport,
        IJobIdAllocator jobIds,
        ISeqAllocator seqs)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _jobIds = jobIds ?? throw new ArgumentNullException(nameof(jobIds));
        _seqs = seqs ?? throw new ArgumentNullException(nameof(seqs));
    }

    public async Task<(ushort jobId, BinaryFrame ack)> StartJobAsync(
        UInt16HbLb type,
        byte flags,
        byte[]? payloadWithoutJobId,
        CancellationToken ct)
    {
        ushort jobId;
        lock (_lock)
        {
            jobId = _jobIds.Next(_inFlight);
            _inFlight.Add(jobId);
        }

        uint seq = _seqs.Next();

        // payload = [jobId u16 LE] + rest
        var payload = new byte[2 + (payloadWithoutJobId?.Length ?? 0)];
        payload[0] = (byte)jobId;
        payload[1] = (byte)(jobId >> 8);
        if (payloadWithoutJobId is { Length: > 0 })
            Buffer.BlockCopy(payloadWithoutJobId, 0, payload, 2, payloadWithoutJobId.Length);

        var frame = new BinaryFrame(
            PayloadLength: new UInt16HbLb { U16Value = (ushort)payload.Length },
            Type: type,
            Flags: flags,
            Seq: seq,
            Payload: payload,
            Crc16: new UInt16HbLb() // placeholder; transport/encoder computes real CRC
        );

        var waitAck = _coordinator.WaitForResponseBySeqAsync(seq, AckTimeout, ct);

        _transport.Send(frame);

        var ack = await waitAck.ConfigureAwait(false);

        // If rejected, free jobId immediately
        if (TryGetAckCode(ack.Payload, out var code) && code != 0)
        {
            lock (_lock) _inFlight.Remove(jobId);
        }

        return (jobId, ack);
    }

    public async Task<BinaryFrame> WaitCompletionAsync(ushort jobId, CancellationToken ct)
    {
        try
        {
            return await _coordinator.WaitForJobCompletionAsync(jobId, JobTimeout, ct)
                                     .ConfigureAwait(false);
        }
        finally
        {
            lock (_lock) _inFlight.Remove(jobId);
        }
    }

    private static bool TryGetAckCode(byte[] payload, out byte ackCode)
    {
        if (payload is null || payload.Length < 3)
        {
            ackCode = 0;
            return false;
        }

        ackCode = payload[2];
        return true;
    }
}