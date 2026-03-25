using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record GenerateCsrResponse(
        RpcStatus Status,
        string DeviceId,
        ushort? CsrDerLen,
        byte[]? CsrDer);
}
