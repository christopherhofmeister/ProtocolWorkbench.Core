using Shp.Device.Provisioning.Dtos.Enums;

namespace ProtocolWorkbench.Core.Services.TrustPolicy
{
    public interface ISecureTrustPolicyStore
    {
        Task SavePolicyAsync(IntermediateCertificatePurpose purpose, byte[] raw100Bytes);

        Task<TrustPolicy?> GetCurrentPolicyAsync(IntermediateCertificatePurpose purpose);
    }
}