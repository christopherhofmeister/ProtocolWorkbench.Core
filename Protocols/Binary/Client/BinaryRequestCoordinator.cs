namespace ProtocolWorkbench.Core.Protocols.Binary.Client
{
    using ProtocolWorkbench.Core.Protocols.Binary.Contracts;
    using ProtocolWorkbench.Core.Protocols.Binary.Frames;
    using System.Collections.Concurrent;

    public sealed class BinaryRequestCoordinator : IFrameRouter
    {
        // SEQ -> response completion (ACK path)
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<BinaryFrame>> _pendingBySeq = new();

        // JobId -> completion notification data (completion path)
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<BinaryFrame>> _pendingByJob = new();

        public void OnFrame(BinaryFrame frame)
        {
            // 1) Normal response path by SEQ
            if ((frame.Flags & 0b0000_0001) != 0) // IS_RESPONSE
            {
                if (_pendingBySeq.TryRemove(frame.Seq, out var tcs))
                {
                    tcs.TrySetResult(frame);
                    return;
                }
            }

            // 2) Notification completion path by JobId
            if ((frame.Flags & 0b0000_0010) != 0) // IS_NOTIFICATION
            {
                if (TryReadJobId(frame.Payload, out var jobId) &&
                    _pendingByJob.TryRemove(jobId, out var jobTcs))
                {
                    jobTcs.TrySetResult(frame);
                    return;
                }
            }

            // else: unhandled frame (log/event/etc)
        }

        public Task<BinaryFrame> WaitForResponseBySeqAsync(uint seq, TimeSpan timeout, CancellationToken ct)
            => WaitAsync(_pendingBySeq, seq, timeout, ct);

        public Task<BinaryFrame> WaitForJobCompletionAsync(ushort jobId, TimeSpan timeout, CancellationToken ct)
            => WaitAsync(_pendingByJob, jobId, timeout, ct);

        private static async Task<BinaryFrame> WaitAsync<TKey>(
            ConcurrentDictionary<TKey, TaskCompletionSource<BinaryFrame>> map,
            TKey key,
            TimeSpan timeout,
            CancellationToken ct) where TKey : notnull
        {
            var tcs = new TaskCompletionSource<BinaryFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!map.TryAdd(key, tcs))
                throw new InvalidOperationException($"Duplicate pending key: {key}");

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await using (linked.Token.Register(() => tcs.TrySetCanceled(linked.Token)))
                    return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                map.TryRemove(key, out _);
            }
        }

        private static bool TryReadJobId(byte[] payload, out ushort jobId)
        {
            if (payload is null || payload.Length < 2)
            {
                jobId = 0;
                return false;
            }

            jobId = (ushort)(payload[0] | (payload[1] << 8)); // LE
            return true;
        }
    }
}
