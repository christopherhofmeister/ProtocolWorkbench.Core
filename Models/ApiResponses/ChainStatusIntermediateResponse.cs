using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record ChainStatusIntermediateResponse(
        RpcStatus Status,
        Guid? Id,
        string? Name,
        string? ThumbprintHex,
        DateTime? NotBeforeUtc,
        DateTime? NotAfterUtc,
        bool? IsActive,
        uint? Version);
}
