using System.Security.Cryptography;

namespace ProtocolWorkbench.Core.Services.Security
{
    public interface ISecuritySessionState
    {
        uint NextSecureTxSeq();
        ECDiffieHellman? SpEcdh { get; set; }
        string? SpPublicB64 { get; set; }
        byte[]? SpNonce { get; set; }
        string? SpNonceB64 { get; }
        Task<byte[]> EnsureSpNonceAsync();
        Task<string> EnsureSpNonceB64Async();
        Task<string> EnsureSpEphemeralPublicB64Async();
        Task LoadAsync();
        Task ResetAsync();
        Task SaveAsync();
    }
}