namespace ProtocolWorkbench.Core.Services.Security
{
    public interface IRootCertificateProvider
    {
        byte[] GetRootCaDer();
        byte[] GetIntermediateCaDer();
    }
}