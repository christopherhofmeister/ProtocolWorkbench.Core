namespace ProtocolWorkbench.Core.Protocols.McuMgr.Transports
{
    public interface IMcuMgrTransport
    {
        int Mtu { get; }

        Task<byte[]> SendAsync(byte[] request, CancellationToken cancellationToken = default);
    }
}
