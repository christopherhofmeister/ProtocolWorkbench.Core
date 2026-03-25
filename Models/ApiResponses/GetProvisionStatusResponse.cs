using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Models.ApiResponses
{
    public sealed record GetProvisionStatusResponse(
        RpcStatus Status,
        byte? ProvisioningState,
        bool? HasDeviceKey,
        bool? HasDeviceCert,
        ushort? DeviceCertLen);
}
