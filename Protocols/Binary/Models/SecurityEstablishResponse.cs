namespace ProtocolWorkbench.Core.Protocols.Binary.Models
{
    public sealed record SecurityEstablishResponse(
    byte Status,
    byte[] ShpDeviceCert,
    byte[] ShpEcdhPub,
    byte[] Signature);
}
