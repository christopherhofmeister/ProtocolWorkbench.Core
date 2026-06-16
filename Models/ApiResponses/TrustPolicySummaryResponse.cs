using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record TrustPolicySummaryResponse(
        RpcStatus Status,
        Guid? Id,
        uint? Version,
        Guid? IntermediateCertificateId,
        DateTime? CreatedUtc,
        DateTime? PublishedUtc,
        bool? IsActive);
}
