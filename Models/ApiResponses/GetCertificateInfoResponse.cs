using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record GetCertificateInfoResponse(
       RpcStatus Status,
       ushort? DeviceCertLen,
       byte[]? DeviceCertDer);
}
