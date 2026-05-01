using ProtocolWorkbench.Core.Protocols.Binary.Models;
using Shp.Device.Provisioning.Dtos.Enums;

namespace ProtocolWorkbench.Core.Services.Security
{
    public interface ISecurityService
    {
        Task ProcessEstablishResponseAsync(uint seq, SecurityEstablishResponse resp, IntermediateCertificatePurpose purpose);
        Task RecordPendingEstablishAsync(uint seq, byte protocolVersion, byte suiteId, byte[] spNonce16, byte[] spPub65);
        bool TryDecryptSecureFrame_ChaCha20Poly1305(
            byte[] wire,
            byte[] key32,
            byte[] nonceBase12,
            out ushort typeValue,
            out byte flags,
            out uint seq,
            out byte[] plaintextPayload);
    }
}