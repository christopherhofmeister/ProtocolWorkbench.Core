namespace ProtocolWorkbench.Core.Protocols.Binary.Contracts
{
    public interface IJobClient
    {
        Task<ushort> StartJobAsync(ushort type, byte flags, byte[] payloadWithoutJobId, CancellationToken ct);
        Task<byte[]> WaitJobAsync(ushort jobId, CancellationToken ct);
    }
}
