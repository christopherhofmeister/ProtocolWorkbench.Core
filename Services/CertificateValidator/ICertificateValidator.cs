namespace ProtocolWorkbench.Services.Security
{
    public interface ICertificateValidator
    {
        CertificateValidationResult ValidateDeviceCertificate(
        byte[] deviceCertDer,
        byte[] rootCaCertDer,        // Ensure this is 'rootCaCertDer'
        byte[]? intermediateCertDer = null); // Ensure this is here too
    }
}