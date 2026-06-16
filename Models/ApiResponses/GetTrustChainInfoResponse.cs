using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record GetTrustChainInfoResponse(
        RpcStatus Status,
        ushort? TrustChainLen,
        byte[]? TrustChainDer);
}
