using System.Security.Cryptography.X509Certificates;

namespace ProtocolWorkbench.Core.Services.CertificateValidator
{
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
                X509Certificate2? intermediateCert = null;

                try
                {
                    using var chain = new X509Chain();

                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);

                    if (intermediateCertDer is not null && intermediateCertDer.Length > 0)
                    {
                        intermediateCert = X509CertificateLoader.LoadCertificate(intermediateCertDer);
                        chain.ChainPolicy.ExtraStore.Add(intermediateCert);
                    }

                    bool ok = chain.Build(deviceCert);

                    System.Diagnostics.Debug.WriteLine($"[CHAIN] Build result={ok}");
                    System.Diagnostics.Debug.WriteLine($"[CHAIN] Root Subject={rootCert.Subject}");
                    System.Diagnostics.Debug.WriteLine($"[CHAIN] Root Thumbprint={rootCert.Thumbprint}");

                    if (intermediateCert is not null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CHAIN] Extra Intermediate Subject={intermediateCert.Subject}");
                        System.Diagnostics.Debug.WriteLine($"[CHAIN] Extra Intermediate Thumbprint={intermediateCert.Thumbprint}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[CHAIN] No intermediate supplied.");
                    }

                    foreach (var element in chain.ChainElements)
                    {
                        System.Diagnostics.Debug.WriteLine("[CHAIN] -----");
                        System.Diagnostics.Debug.WriteLine($"[CHAIN] Subject={element.Certificate.Subject}");
                        System.Diagnostics.Debug.WriteLine($"[CHAIN] Issuer={element.Certificate.Issuer}");
                        System.Diagnostics.Debug.WriteLine($"[CHAIN] Thumbprint={element.Certificate.Thumbprint}");

                        foreach (var status in element.ChainElementStatus)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CHAIN] Status={status.Status}");
                            System.Diagnostics.Debug.WriteLine($"[CHAIN] Info={status.StatusInformation?.Trim()}");
                        }
                    }

                    if (!ok)
                    {
                        string error = FormatChainErrors(chain);

                        deviceCert.Dispose();
                        rootCert.Dispose();
                        intermediateCert?.Dispose();

                        return CertificateValidationResult.Fail(error);
                    }

                    rootCert.Dispose();
                    intermediateCert?.Dispose();

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
}