using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using ProtocolWorkbench.Core.Protocols.Binary.Models;
using ProtocolWorkbench.Core.Services.CertificateValidator;
using ProtocolWorkbench.Core.Services.TrustPolicy;
using Shp.Device.Provisioning.Dtos.Enums;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TrustPolicyModel = ProtocolWorkbench.Core.Services.TrustPolicy.TrustPolicy;


namespace ProtocolWorkbench.Core.Services.Security
{
    public sealed class SecurityService : ISecurityService
    {
        private readonly ICertificateValidator _certificateValidator;
        private readonly IRootCertificateProvider _rootCertificateProvider;
        private readonly ISecureTrustPolicyStore _secureTrustPolicyStore;
        private readonly SecuritySessionState _state; // use concrete type so we can call SaveEstablishedSessionAsync
        private readonly object _pendingLock = new();

        public SecurityService(ISecuritySessionState state, ICertificateValidator certificateValidator,
            IRootCertificateProvider rootCertificateProvider, ISecureTrustPolicyStore secureTrustPolicyStore)
        {
            // If DI is giving you the interface, you can cast (or register concrete)
            _state = state as SecuritySessionState
                ?? throw new InvalidOperationException("SecurityService requires SecuritySessionState concrete type (for now).");
            _certificateValidator = certificateValidator ?? throw new ArgumentNullException(nameof(certificateValidator));
            _rootCertificateProvider = rootCertificateProvider ?? throw new ArgumentNullException(nameof(rootCertificateProvider));
            _secureTrustPolicyStore = secureTrustPolicyStore ?? throw new ArgumentNullException(nameof(secureTrustPolicyStore));
        }

        private sealed class PendingEstablish
        {
            public byte ProtocolVersion { get; init; }
            public byte SuiteId { get; init; }
            public byte[] SpNonce16 { get; init; } = Array.Empty<byte>();
            public byte[] SpPub65 { get; init; } = Array.Empty<byte>();

            // NEW: snapshot the private key for this establish
            public ECParameters SpPrivateParams { get; init; }  // includes D
        }

        private readonly Dictionary<uint, PendingEstablish> _pending = new();

        static byte[] BuildTranscript(byte protocolVersion, byte suiteId, byte[] spNonce16, byte[] spPub65, byte[] shpPub65)
        {
            var t = new byte[1 + 1 + 16 + 65 + 65];
            int o = 0;
            t[o++] = protocolVersion;
            t[o++] = suiteId;
            Buffer.BlockCopy(spNonce16, 0, t, o, 16); o += 16;
            Buffer.BlockCopy(spPub65, 0, t, o, 65); o += 65;
            Buffer.BlockCopy(shpPub65, 0, t, o, 65); o += 65;
            return t;
        }

        private PendingEstablish GetPendingOrThrow(uint seq)
        {
            lock (_pendingLock)
            {
                if (_pending.TryGetValue(seq, out var pend))
                    return pend;
            }

            throw new InvalidOperationException($"No pending establish for seq={seq} (did you record it when sending?)");
        }

        private void RemovePending(uint seq)
        {
            lock (_pendingLock)
            {
                _pending.Remove(seq);
            }
        }

        private static void LogHandshakeInputs(PendingEstablish pend, SecurityEstablishResponse resp, byte[] transcript, byte[] transcriptHash)
        {
            Debug.WriteLine($"[EST] spNonce16={Convert.ToHexString(pend.SpNonce16)}");
            Debug.WriteLine($"[EST] spPub65={Hex(pend.SpPub65, 65)}");
            Debug.WriteLine($"[EST] shpPub65={Hex(resp.ShpEcdhPub, 65)}");
            Debug.WriteLine($"[EST] sig64={Hex(resp.Signature, 64)}");

            Debug.WriteLine($"[EST] transcriptLen={transcript.Length} transcriptFirst={Hex(transcript, 80)}");
            Debug.WriteLine($"[EST] transcriptHash={Convert.ToHexString(transcriptHash)}");

            static string Hex(ReadOnlySpan<byte> b, int max)
            {
                int n = Math.Min(b.Length, max);
                return Convert.ToHexString(b[..n].ToArray());
            }
        }

        private static void VerifyEstablishSignatureOrThrow(X509Certificate2 cert, SecurityEstablishResponse resp, byte[] transcriptHash)
        {
            using ECDsa? pub = cert.GetECDsaPublicKey();
            if (pub is null)
                throw new InvalidOperationException("Cert does not contain an ECDSA public key.");

            var certPub = ExportCertPubSec1(cert);
            Debug.WriteLine($"[SEC] CERT PUB SEC1: {Convert.ToHexString(certPub)}");

            bool ok = pub.VerifyHash(
                transcriptHash,
                resp.Signature,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

            Debug.WriteLine($"[EST] signatureOK={ok}");

            if (!ok)
                throw new InvalidOperationException("Establish signature verification FAILED.");
        }

        private static byte[] DeriveSharedSecretRawP256_X(ECParameters spPrivateParams, byte[] shpPubSec1_65)
        {
            if (shpPubSec1_65 is null || shpPubSec1_65.Length != 65 || shpPubSec1_65[0] != 0x04)
                throw new ArgumentException("SHP pub must be SEC1 uncompressed 65 bytes.", nameof(shpPubSec1_65));

            if (spPrivateParams.D is null || spPrivateParams.D.Length != 32)
                throw new ArgumentException("SP private D must be 32 bytes.", nameof(spPrivateParams));

            // P-256 domain params
            var x9 = SecNamedCurves.GetByName("secp256r1"); // aka prime256v1 / nistP256
            var domain = new ECDomainParameters(x9.Curve, x9.G, x9.N, x9.H, x9.GetSeed());

            // Private key (D is big-endian)
            var d = new BigInteger(1, spPrivateParams.D);
            var priv = new ECPrivateKeyParameters(d, domain);

            // Peer public key from SEC1 04||X||Y (big-endian)
            Org.BouncyCastle.Math.EC.ECPoint q = x9.Curve.DecodePoint(shpPubSec1_65);
            var pub = new ECPublicKeyParameters(q, domain);

            // ECDH basic agreement returns the X coordinate as a BigInteger
            var agree = new ECDHBasicAgreement();
            agree.Init(priv);

            BigInteger sharedX = agree.CalculateAgreement(pub);

            // Convert to fixed 32-byte big-endian
            byte[] xBytes = sharedX.ToByteArrayUnsigned();
            if (xBytes.Length == 32) return xBytes;

            var out32 = new byte[32];
            if (xBytes.Length > 32)
            {
                // Shouldn't happen for P-256, but be safe.
                Buffer.BlockCopy(xBytes, xBytes.Length - 32, out32, 0, 32);
            }
            else
            {
                // Left pad with zeros
                Buffer.BlockCopy(xBytes, 0, out32, 32 - xBytes.Length, xBytes.Length);
            }
            return out32;
        }

        private enum HkdfInfoEncoding
        {
            AsciiNoNul,
            AsciiWithNul
        }

        private static byte[] HkdfExtract(byte[] salt, byte[] ikm)
        {
            using var hmac = new HMACSHA256(salt);
            return hmac.ComputeHash(ikm);
        }

        private static byte[] HkdfExpand(byte[] prk, byte[] info, int length)
        {
            using var hmac = new HMACSHA256(prk);

            var result = new byte[length];
            var t = Array.Empty<byte>();
            int pos = 0;
            byte counter = 1;

            while (pos < length)
            {
                hmac.Initialize();

                var data = new byte[t.Length + info.Length + 1];
                Buffer.BlockCopy(t, 0, data, 0, t.Length);
                Buffer.BlockCopy(info, 0, data, t.Length, info.Length);
                data[^1] = counter;

                t = hmac.ComputeHash(data);

                int toCopy = Math.Min(t.Length, length - pos);
                Buffer.BlockCopy(t, 0, result, pos, toCopy);

                pos += toCopy;
                counter++;
            }

            return result;
        }

        private readonly record struct SessionMaterial(
            byte[] KeySpToShp16,
            byte[] KeyShpToSp16,
            byte[] NonceBaseSpToShp13,
            byte[] NonceBaseShpToSp13);

        private static SessionMaterial DeriveSessionMaterial(
            byte[] sharedSecret,
            byte[] transcriptHash)
        {
            var prk = HkdfExtract(transcriptHash, sharedSecret);

            byte[] keySpToShp =
                HkdfExpand(prk, Encoding.ASCII.GetBytes("SP->SHP key"), 16);

            byte[] keyShpToSp =
                HkdfExpand(prk, Encoding.ASCII.GetBytes("SHP->SP key"), 16);

            byte[] nonceSpToShp =
                HkdfExpand(prk, Encoding.ASCII.GetBytes("SP->SHP nonce"), 13);

            byte[] nonceShpToSp =
                HkdfExpand(prk, Encoding.ASCII.GetBytes("SHP->SP nonce"), 13);

            return new SessionMaterial(
                keySpToShp,
                keyShpToSp,
                nonceSpToShp,
                nonceShpToSp);
        }

        public async Task ProcessEstablishResponseAsync(uint seq, SecurityEstablishResponse resp, IntermediateCertificatePurpose purpose)
        {
            if (resp is null)
                throw new ArgumentNullException(nameof(resp));

            if (resp.Status != 0)
                throw new InvalidOperationException($"Establish Session failed: status={resp.Status}");

            PendingEstablish pending = GetPendingOrThrow(seq);

            TrustPolicyModel trustPolicy =
                await _secureTrustPolicyStore.GetCurrentPolicyAsync(purpose)
                ?? throw new InvalidOperationException("No Trust Policy found in secure storage. Handshake aborted.");

            try
            {
                byte[] transcript = BuildTranscript(
                    pending.ProtocolVersion,
                    pending.SuiteId,
                    pending.SpNonce16,
                    pending.SpPub65,
                    resp.ShpEcdhPub);

                byte[] transcriptHash = SHA256.HashData(transcript);

                LogHandshakeInputs(pending, resp, transcript, transcriptHash);
                LogCertificateInputs(resp);

                byte[] rootCertDer = GetRootForPurpose(purpose);
                byte[] intermediateCertDer = GetIntermediateForPurpose(purpose);

                Debug.WriteLine($"[CERT PURPOSE] {purpose}");
                Debug.WriteLine($"[INT DER SHA256] {Convert.ToHexString(SHA256.HashData(intermediateCertDer))}");

                using var rootCert = X509CertificateLoader.LoadCertificate(rootCertDer);
                using var intermediateCert = X509CertificateLoader.LoadCertificate(intermediateCertDer);
                using var responseCert = X509CertificateLoader.LoadCertificate(resp.ShpDeviceCert);

                Debug.WriteLine($"[ROOT] Subject={rootCert.Subject}");
                Debug.WriteLine($"[ROOT] Thumbprint={rootCert.Thumbprint}");
                Debug.WriteLine($"[INT] Subject={intermediateCert.Subject}");
                Debug.WriteLine($"[INT] Thumbprint={intermediateCert.Thumbprint}");
                Debug.WriteLine($"[CERT] Subject={responseCert.Subject}");
                Debug.WriteLine($"[CERT] Issuer={responseCert.Issuer}");
                Debug.WriteLine($"[CERT] Thumbprint={responseCert.Thumbprint}");
                Debug.WriteLine($"[CERT] NotBefore={responseCert.NotBefore:o}");
                Debug.WriteLine($"[CERT] NotAfter={responseCert.NotAfter:o}");

                var certResult = _certificateValidator.ValidateDeviceCertificate(
                    deviceCertDer: resp.ShpDeviceCert,
                    rootCaCertDer: rootCertDer,
                    intermediateCertDer: intermediateCertDer);

                if (!certResult.IsValid)
                    throw new InvalidOperationException($"Device certificate validation failed: {certResult.Error}");

                using var deviceCert = certResult.DeviceCertificate!;

                VerifyTrustPolicyPin(deviceCert, trustPolicy, intermediateCertDer, rootCertDer);

                Debug.WriteLine($"[SEC] Policy Match: Version {trustPolicy.Version} enforced.");

                VerifyEstablishSignatureOrThrow(deviceCert, resp, transcriptHash);

                byte[] sharedSecret = DeriveSharedSecretRawP256_X(pending.SpPrivateParams, resp.ShpEcdhPub);

                var derived = DeriveSessionMaterial(sharedSecret, transcriptHash);

                await _state.SaveEstablishedSessionAsync(
                    transcriptHash32: transcriptHash,
                    keySpToShp16: derived.KeySpToShp16,
                    keyShpToSp16: derived.KeyShpToSp16,
                    nonceBaseSpToShp13: derived.NonceBaseSpToShp13,
                    nonceBaseShpToSp13: derived.NonceBaseShpToSp13);

                RemovePending(seq);

                Debug.WriteLine("[SEC] Security Session Established successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SEC] Handshake Failed: {ex.Message}");
                throw;
            }
        }

        private static void LogCertificateInputs(SecurityEstablishResponse resp)
        {
            using var cert = X509CertificateLoader.LoadCertificate(resp.ShpDeviceCert);

            Debug.WriteLine($"[CERT] Device cert len={resp.ShpDeviceCert.Length}");
            Debug.WriteLine($"[CERT] Subject={cert.Subject}");
            Debug.WriteLine($"[CERT] Issuer={cert.Issuer}");
            Debug.WriteLine($"[CERT] Thumbprint={cert.Thumbprint}");
            Debug.WriteLine($"[CERT] NotBefore={cert.NotBefore:o}");
            Debug.WriteLine($"[CERT] NotAfter={cert.NotAfter:o}");
        }

        static byte[] ExportCertPubSec1(X509Certificate2 cert)
        {
            using var pub = cert.GetECDsaPublicKey();
            if (pub is null) throw new InvalidOperationException("Cert has no ECDSA public key.");

            var p = pub.ExportParameters(false);
            if (p.Q.X is null || p.Q.Y is null) throw new InvalidOperationException("Cert pub missing X/Y.");

            var sec1 = new byte[65];
            sec1[0] = 0x04;
            Buffer.BlockCopy(p.Q.X, 0, sec1, 1, 32);
            Buffer.BlockCopy(p.Q.Y, 0, sec1, 33, 32);
            return sec1;
        }

        public async Task RecordPendingEstablishAsync(uint seq, byte protocolVersion, byte suiteId, byte[] spNonce16, byte[] spPub65)
        {
            if (spNonce16 is null || spNonce16.Length != 16)
                throw new ArgumentException("spNonce16 must be 16 bytes.", nameof(spNonce16));

            if (spPub65 is null || spPub65.Length != 65)
                throw new ArgumentException("spPub65 must be 65 bytes.", nameof(spPub65));

            if (_state.SpEcdh is null)
                throw new InvalidOperationException(
                    "SP ECDH keypair missing before RecordPending. It must be created before building the payload.");

            // Export current keypair and PIN it to this SEQ
            var sp = _state.SpEcdh.ExportParameters(includePrivateParameters: true);

            if (sp.D is null || sp.D.Length != 32)
                throw new InvalidOperationException("SP private scalar D missing/wrong size (expected 32 bytes for P-256).");

            if (sp.Q.X is null || sp.Q.Y is null || sp.Q.X.Length != 32 || sp.Q.Y.Length != 32)
                throw new InvalidOperationException("SP public Q missing/wrong size (expected 32-byte X/Y).");

            // Rebuild SEC1 pub from exported Q and confirm it matches the payload
            byte[] exportedSpPub65 = new byte[65];
            exportedSpPub65[0] = 0x04;
            Buffer.BlockCopy(sp.Q.X, 0, exportedSpPub65, 1, 32);
            Buffer.BlockCopy(sp.Q.Y, 0, exportedSpPub65, 33, 32);

            if (!spPub65.SequenceEqual(exportedSpPub65))
            {
                Debug.WriteLine($"[EST] RecordPendingEstablishAsync mismatch!");
                Debug.WriteLine($"[EST] payload  spPub65={Convert.ToHexString(spPub65)}");
                Debug.WriteLine($"[EST] exported spPub65={Convert.ToHexString(exportedSpPub65)}");
                throw new InvalidOperationException(
                    "State SP keypair does not match Establish payload SP pub. Keypair rotated or payload used a different key.");
            }

            // Deep-clone ECParameters (arrays)
            var spPrivClone = new ECParameters
            {
                Curve = sp.Curve,
                D = (byte[])sp.D.Clone(),
                Q = new System.Security.Cryptography.ECPoint
                {
                    X = (byte[])sp.Q.X.Clone(),
                    Y = (byte[])sp.Q.Y.Clone()
                }
            };

            var pending = new PendingEstablish
            {
                ProtocolVersion = protocolVersion,
                SuiteId = suiteId,
                SpNonce16 = (byte[])spNonce16.Clone(),
                SpPub65 = (byte[])spPub65.Clone(),
                SpPrivateParams = spPrivClone
            };

            lock (_pendingLock)
                _pending[seq] = pending;

            Debug.WriteLine($"[EST] Pending recorded for seq={seq} (pinned SP private key).");
        }

        public bool TryDecryptSecureFrame_Aes128Ccm(
            byte[] wire,
            byte[] key16,
            byte[] nonceBase13,
            out ushort typeValue,
            out byte flags,
            out uint seq,
            out byte[] plaintextPayload)
        {
            plaintextPayload = Array.Empty<byte>();
            typeValue = 0;
            flags = 0;
            seq = 0;

            const byte SOF = 0xAA;
            const byte EOF = 0x55;
            const int TagSize = 16;

            if (wire.Length < 1 + 2 + 2 + 1 + 4 + TagSize + 1) return false;
            if (wire[0] != SOF) return false;
            if (wire[^1] != EOF) return false;

            int o = 1;

            ushort lenAfterLen = (ushort)(wire[o] | (wire[o + 1] << 8));
            o += 2;

            typeValue = (ushort)(wire[o] | (wire[o + 1] << 8));
            o += 2;

            flags = wire[o++];

            seq = (uint)(wire[o] | (wire[o + 1] << 8) | (wire[o + 2] << 16) | (wire[o + 3] << 24));
            o += 4;

            // total bytes expected: 1 + 2 + lenAfterLen
            int expectedTotal = 1 + 2 + lenAfterLen;
            if (wire.Length != expectedTotal) return false;

            // ciphertext length = lenAfterLen - (TYPE2+FLAGS1+SEQ4+TAG16+EOF1)
            int cipherLen = lenAfterLen - (2 + 1 + 4 + TagSize + 1);
            if (cipherLen < 0) return false;

            int cipherStart = o;
            int tagStart = cipherStart + cipherLen;
            if (tagStart + TagSize != wire.Length - 1) return false;

            ReadOnlySpan<byte> ciphertext = wire.AsSpan(cipherStart, cipherLen);
            ReadOnlySpan<byte> tag = wire.AsSpan(tagStart, TagSize);

            // AAD = LEN + TYPE + FLAGS + SEQ (little endian)
            byte[] aad = new byte[2 + 2 + 1 + 4];
            int a = 0;
            aad[a++] = (byte)(lenAfterLen & 0xFF);
            aad[a++] = (byte)((lenAfterLen >> 8) & 0xFF);
            aad[a++] = (byte)(typeValue & 0xFF);
            aad[a++] = (byte)((typeValue >> 8) & 0xFF);
            aad[a++] = flags;
            aad[a++] = (byte)(seq & 0xFF);
            aad[a++] = (byte)((seq >> 8) & 0xFF);
            aad[a++] = (byte)((seq >> 16) & 0xFF);
            aad[a++] = (byte)((seq >> 24) & 0xFF);

            // nonce = base(13) with last 4 bytes overwritten by SEQ (little-endian)
            byte[] nonce = (byte[])nonceBase13.Clone();
            nonce[9] = (byte)(seq & 0xFF);
            nonce[10] = (byte)((seq >> 8) & 0xFF);
            nonce[11] = (byte)((seq >> 16) & 0xFF);
            nonce[12] = (byte)((seq >> 24) & 0xFF);

            byte[] pt = new byte[cipherLen];
            try
            {
                using var aead = new AesCcm(key16);
                aead.Decrypt(nonce, ciphertext, tag, pt, aad);
                plaintextPayload = pt;
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        private void VerifyTrustPolicyPin(X509Certificate2 deviceCert, TrustPolicyModel policy, byte[] intermediateCertDer, byte[] rootCertDer)
        {
            // Load the certificates using the modern, high-performance loader
            using var rootCert = X509CertificateLoader.LoadCertificate(rootCertDer);
            using var intermediateCertFromDer = X509CertificateLoader.LoadCertificate(intermediateCertDer);

            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

            // Explicitly tell the chain to only trust our provided Root
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(rootCert);

            // Add the Intermediate to the ExtraStore so the chain builder can find it
            chain.ChainPolicy.ExtraStore.Add(intermediateCertFromDer);

            // Build the path: Device -> Intermediate -> Root
            if (!chain.Build(deviceCert))
            {
                // Even if validation isn't 100% (e.g. untrusted root), we need to see the elements
                Debug.WriteLine("[SEC] Chain build returned false, checking elements anyway...");
            }

            if (chain.ChainElements.Count < 2)
            {
                foreach (var element in chain.ChainElements)
                {
                    Debug.WriteLine($"[SEC] Found in chain: {element.Certificate.Subject}");
                }
                throw new InvalidOperationException($"Certificate chain is too short ({chain.ChainElements.Count}). Intermediate not found.");
            }

            // Index 1 is our Intermediate CA
            var intermediateInChain = chain.ChainElements[1].Certificate;

            // Calculate the SHA-256 of the Subject Public Key Info (SPKI)
            byte[] actualSpkiHash = SHA256.HashData(intermediateInChain.PublicKey.ExportSubjectPublicKeyInfo());

            if (!actualSpkiHash.SequenceEqual(policy.AllowedIntermediateSpkiHash))
            {
                Debug.WriteLine($"[SEC] POLICY VIOLATION!");
                Debug.WriteLine($"[SEC] Expected: {Convert.ToHexString(policy.AllowedIntermediateSpkiHash)}");
                Debug.WriteLine($"[SEC] Actual:   {Convert.ToHexString(actualSpkiHash)}");
                throw new System.Security.SecurityException("Trust Policy Violation: Intermediate CA is not authorized.");
            }
        }

        private byte[] GetRootForPurpose(IntermediateCertificatePurpose purpose)
        {
            return purpose switch
            {
                IntermediateCertificatePurpose.DeviceIdentity =>

                    _rootCertificateProvider.GetRootCaDer(),

                IntermediateCertificatePurpose.CommissioningIdentity =>

                    _rootCertificateProvider.GetCommissioningRootCaDer(),

                _ => throw new InvalidOperationException($"Unsupported certificate purpose: {purpose}")
            };
        }

        private byte[] GetIntermediateForPurpose(IntermediateCertificatePurpose purpose)
        {
            return purpose switch
            {
                IntermediateCertificatePurpose.DeviceIdentity =>
                    _rootCertificateProvider.GetIntermediateCaDer(),

                IntermediateCertificatePurpose.CommissioningIdentity =>
                    _rootCertificateProvider.GetCommissioningIntermediateCaDer(),

                _ => throw new InvalidOperationException($"Unsupported certificate purpose: {purpose}")
            };
        }

    }
}