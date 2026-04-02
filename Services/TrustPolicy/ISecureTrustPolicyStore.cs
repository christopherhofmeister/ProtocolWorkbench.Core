namespace ProtocolWorkbench.Core.Services.TrustPolicy
{
    public interface ISecureTrustPolicyStore
    {
        Task<TrustPolicy?> GetCurrentPolicyAsync();
        Task SavePolicyAsync(byte[] raw100Bytes);
    }
}