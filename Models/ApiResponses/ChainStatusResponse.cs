using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record ChainStatusResponse(
        RpcStatus Status,
        string? ChainName,
        bool? IsReady,
        ChainStatusIntermediateResponse? ActiveIntermediate,
        TrustPolicySummaryResponse? ActivePolicy);
}
