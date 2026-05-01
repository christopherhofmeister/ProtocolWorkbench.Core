using ProtocolWorkbench.Core.Services.Security;
using System.Security.Cryptography.X509Certificates;

public sealed class RootCertificateProvider : IRootCertificateProvider
{
    private readonly byte[] _rootCaDer;
    private readonly byte[] _intermediateCaDer;

    private readonly byte[] _commissioningRootCaDer;
    private readonly byte[] _commissioningIntermediateCaDer;

    public RootCertificateProvider()
    {
        // UART / Device Identity
        _rootCaDer = LoadDerFromResource(
            "ProtocolWorkbench.Core.Services.Security.ProvisioningCerts.ca-root.crt");

        _intermediateCaDer = LoadDerFromResource(
            "ProtocolWorkbench.Core.Services.Security.ProvisioningCerts.ca-intermediate.crt");

        // BLE / Commissioning
        _commissioningRootCaDer = LoadDerFromResource(
            "ProtocolWorkbench.Core.Services.Security.ProvisioningCerts.commissioning-root.cert.pem");

        _commissioningIntermediateCaDer = LoadDerFromResource(
            "ProtocolWorkbench.Core.Services.Security.ProvisioningCerts.commissioning-intermediate.cert.pem");
    }

    public byte[] GetRootCaDer() => _rootCaDer;

    public byte[] GetIntermediateCaDer() => _intermediateCaDer;

    public byte[] GetCommissioningRootCaDer() => _commissioningRootCaDer;

    public byte[] GetCommissioningIntermediateCaDer() => _commissioningIntermediateCaDer;

    private static byte[] LoadDerFromResource(string resourceName)
    {
        var assembly = typeof(RootCertificateProvider).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        string pem = reader.ReadToEnd();

        using var cert = X509Certificate2.CreateFromPem(pem);
        return cert.Export(X509ContentType.Cert);
    }
}