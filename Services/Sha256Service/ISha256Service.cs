namespace ProtocolWorkbench.Core.Services.Sha256Service
{
    public interface ISha256Service
    {
        (string SHA256String, byte[] SHA256, ulong FileSize) GenerateSHA256FromFile(string filePath);
        string Sha256BytesToString(byte[] sha256);
    }
}