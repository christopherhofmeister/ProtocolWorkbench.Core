using System.Security.Cryptography.X509Certificates;

namespace ProtocolWorkbench.Services.Security;

public sealed class CertificateValidator : ICertificateValidator
{
    public CertificateValidationResult ValidateDeviceCertificate(
        byte[] deviceCertDer,
        byte[] rootCaCertDer,
        byte[]? intermediateCertDer = null)
    {
        if (deviceCertDer is null || deviceCertDer.Length == 0)
            return CertificateValidationResult.Fail("deviceCertDer is null or empty.");

        if (rootCaCertDer is null || rootCaCertDer.Length == 0)
            return CertificateValidationResult.Fail("rootCaCertDer is null or empty.");

        try
        {
            var deviceCert = X509CertificateLoader.LoadCertificate(deviceCertDer);
            var rootCert = X509CertificateLoader.LoadCertificate(rootCaCertDer);

            using var chain = new X509Chain();

            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

            // Add the root certificate to the trust store
            chain.ChainPolicy.CustomTrustStore.Add(rootCert);

            X509Certificate2? intermediateCert = null;
            try
            {
                if (intermediateCertDer is not null && intermediateCertDer.Length > 0)
                {
                    intermediateCert = X509CertificateLoader.LoadCertificate(intermediateCertDer);
                    chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                }

                bool ok = chain.Build(deviceCert);
                if (!ok)
                {
                    deviceCert.Dispose();
                    rootCert.Dispose();
                    intermediateCert?.Dispose();

                    return CertificateValidationResult.Fail(FormatChainErrors(chain));
                }

                // Success: caller takes ownership of deviceCert
                return CertificateValidationResult.Success(deviceCert);
            }
            catch
            {
                deviceCert.Dispose();
                rootCert.Dispose();
                intermediateCert?.Dispose();
                throw;
            }
        }
        catch (Exception ex)
        {
            return CertificateValidationResult.Fail(ex.Message);
        }
    }

    private static string FormatChainErrors(X509Chain chain)
    {
        if (chain.ChainStatus is null || chain.ChainStatus.Length == 0)
            return "Unknown certificate chain failure.";

        return string.Join(
            " | ",
            chain.ChainStatus.Select(s => $"{s.Status}: {s.StatusInformation?.Trim()}"));
    }
}

public sealed record CertificateValidationResult(
    bool IsValid,
    X509Certificate2? DeviceCertificate,
    string Error)
{
    public static CertificateValidationResult Success(X509Certificate2 deviceCertificate) =>
        new(true, deviceCertificate, string.Empty);

    public static CertificateValidationResult Fail(string error) =>
        new(false, null, error);
}