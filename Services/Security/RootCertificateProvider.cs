using System.Security.Cryptography.X509Certificates;

namespace ProtocolWorkbench.Core.Services.Security
{

    public sealed class RootCertificateProvider : IRootCertificateProvider
    {
        private readonly byte[] _rootCaDer;
        private readonly byte[] _intermediateCaDer;

        public RootCertificateProvider()
        {
            // Load the Root CA 
            _rootCaDer = LoadDerFromResource("ProtocolWorkbench.Core.Services.Security.ProvisioningCerts.ca-root.crt");

            // Load the Intermediate CA 
            _intermediateCaDer = LoadDerFromResource("ProtocolWorkbench.Core.Services.Security.ProvisioningCerts.ca-intermediate.crt");
        }

        public byte[] GetRootCaDer() => _rootCaDer;
        public byte[] GetIntermediateCaDer() => _intermediateCaDer;

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
}