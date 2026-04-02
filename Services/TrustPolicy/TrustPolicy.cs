namespace ProtocolWorkbench.Core.Services.TrustPolicy
{
    public class TrustPolicy
    {
        public uint Version { get; set; }
        public byte[] AllowedIntermediateSpkiHash { get; set; } = new byte[32];
        public byte[] RootSignature { get; set; } = new byte[64];
        public byte[] Raw { get; set; } = new byte[100];
    }
}
