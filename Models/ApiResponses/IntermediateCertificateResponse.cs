using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record IntermediateCertificateResponse(
        RpcStatus Status,
        Guid? Id,
        string? Name,
        string? ThumbprintHex,
        byte[]? SpkiHash,
        DateTime? CreatedUtc,
        DateTime? NotBeforeUtc,
        DateTime? NotAfterUtc,
        bool? IsActive,
        uint? Version);
}
