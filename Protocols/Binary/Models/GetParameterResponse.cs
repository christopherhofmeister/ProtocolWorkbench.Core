using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Protocols.Binary.Models
{
    public sealed record GetParameterResponse(
        RpcStatus Status,
        byte ParameterId,
        CTypes? ValueType,
        uint? ValueLen,
        object? Value
    );
}
