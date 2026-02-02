using ProtocolWorkbench.Core.Protocols.Binary.Frames;

namespace ProtocolWorkbench.Core.Protocols.Binary.Contracts
{
    public interface IFrameRouter
    {
        // Feed decoded frames here (from decoder)
        void OnFrame(BinaryFrame frame);
        Task<BinaryFrame> WaitForResponseBySeqAsync(uint seq, TimeSpan timeout, CancellationToken ct);
        Task<BinaryFrame> WaitForJobCompletionAsync(ushort jobId, TimeSpan timeout, CancellationToken ct);
    }
}
