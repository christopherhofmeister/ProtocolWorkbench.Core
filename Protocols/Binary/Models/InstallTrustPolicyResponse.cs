using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Protocols.Binary.Models
{
    /// <summary>
    /// Represents the response from the SHP after an 'Install Trust Policy' command.
    /// </summary>
    /// <param name="Status">The result of the operation (0 = Ok, non-zero = Error).</param>
    public record InstallTrustPolicyResponse(RpcStatus Status);
}
