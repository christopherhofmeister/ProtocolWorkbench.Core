using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Protocols.Binary.Models
{
    public sealed record InstallSetupCodeResponse(RpcStatus Status, string DeviceId);
}
