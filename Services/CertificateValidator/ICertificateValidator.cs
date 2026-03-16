namespace ProtocolWorkbench.Services.Security
{
    public interface ICertificateValidator
    {
        CertificateValidationResult ValidateDeviceCertificate(byte[] deviceCertDer, byte[] rootCaCertDer, byte[]? intermediateCertDer = null);
    }
}