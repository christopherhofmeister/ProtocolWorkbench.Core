using ProtocolWorkbench.Core.Enums;
using ProtocolWorkbench.Core.Models.ApiResponses;
using ProtocolWorkbench.Core.Protocols.Binary.Models;
using System.Buffers.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class ManufacturingTecPayloadDecoder
    {
        public static byte[]? Csr { get; set; }

        public static byte[]? CommissioningCsr { get; set; }

        public static string? CommissioningCertificatePem { get; set; }

        public static string? CommissioningDeviceId { get; set; }

        public static string CommissioningCsrAsBase64()
        {
            if (CommissioningCsr == null || CommissioningCsr.Length == 0)
                return string.Empty;

            return Convert.ToBase64String(CommissioningCsr);
        }

        public static string? CommissioningCertificatePemAsBase64()
        {
            if (string.IsNullOrWhiteSpace(CommissioningCertificatePem))
                return string.Empty;

            using var cert = X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(CommissioningCertificatePem));
            byte[] rawDerBytes = cert.RawData;
            return Convert.ToBase64String(rawDerBytes);
        }

        /// <summary>
        /// Stores the 100-byte Base64-encoded Trust Policy fetched from the Cloud API.
        /// This is used by the AutoGen service to populate the UART command fields.
        /// </summary>
        public static string? CurrentTrustPolicyB64 { get; set; }

        public static string CsrAsBase64()
        {
            if (Csr == null || Csr.Length == 0)
                return string.Empty;

            return Convert.ToBase64String(Csr);
        }

        public static string? CertificatePem { get; set; }

        public static string? CertificatePemAsBase64()
        {
            if (string.IsNullOrWhiteSpace(CertificatePem))
                return string.Empty;

            // 1. Load the PEM into a proper .NET Cert object (this validates it too!)
            using var cert = X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(CertificatePem));

            // 2. Get the RAW binary (DER) bytes
            byte[] rawDerBytes = cert.RawData;

            // 3. Convert ONLY the raw bytes to a clean Base64 string for the UI field
            return Convert.ToBase64String(rawDerBytes);
        }

        public static string? DeviceId { get; set; }

        public static byte[]? ServerWifiCsr { get; set; }

        public static string? ServerWifiDeviceId { get; set; }

        public static string? ServerWifiCertificatePem { get; set; }

        /// <summary>
        /// The CA certificate chain PEM for server-WiFi mTLS trust, fetched from the
        /// provisioning server via GetGenerationsAsync(CertificateChainName.ShpServerWifi).
        /// Contains only trust-anchor certs (intermediates + root), NOT the device leaf.
        /// Populated by MethodRunnerViewModel after the server-WiFi CSR is signed.
        /// </summary>
        public static string? ServerWifiCertificateChainPem { get; set; }

        public static string ServerWifiCsrAsBase64()
        {
            if (ServerWifiCsr == null || ServerWifiCsr.Length == 0)
                return string.Empty;

            return Convert.ToBase64String(ServerWifiCsr);
        }

        public static string? ServerWifiCertificatePemAsBase64()
        {
            if (string.IsNullOrWhiteSpace(ServerWifiCertificatePem))
                return string.Empty;

            using var cert = X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(ServerWifiCertificatePem));
            byte[] rawDerBytes = cert.RawData;
            return Convert.ToBase64String(rawDerBytes);
        }

        /// <summary>
        /// Returns the base64-encoded concatenated DER bytes of the server-WiFi CA chain.
        /// Prefers <see cref="ServerWifiCertificateChainPem"/> (the CA-chain-only PEM fetched
        /// from the provisioning server via GetGenerationsAsync), which includes ALL certs.
        /// Falls back to extracting the chain from <see cref="ServerWifiCertificatePem"/>
        /// (the signing response PEM) by skipping the leaf cert (index 0).
        /// Returns <c>null</c> if neither source provides chain data.
        /// </summary>
        public static string? ServerWifiCertificateChainAsBase64()
        {
            // Prefer the CA chain fetched from the provisioning server.
            // This PEM contains only trust-anchor certs (no device leaf), so include ALL.
            if (!string.IsNullOrWhiteSpace(ServerWifiCertificateChainPem))
            {
                byte[]? fetchedChain = ExtractAllCertsDerBytes(ServerWifiCertificateChainPem);
                return (fetchedChain is { Length: > 0 }) ? Convert.ToBase64String(fetchedChain) : null;
            }

            // Fallback: signing API may return leaf + chain in one PEM; skip the leaf.
            if (string.IsNullOrWhiteSpace(ServerWifiCertificatePem))
                return null;

            byte[]? chainBytes = ExtractChainDerBytes(ServerWifiCertificatePem);
            return (chainBytes is { Length: > 0 }) ? Convert.ToBase64String(chainBytes) : null;
        }

        /// <summary>
        /// Parses a PEM string that may contain multiple -----BEGIN CERTIFICATE----- blocks.
        /// Skips the first (leaf) cert and returns the concatenated DER bytes of all remaining
        /// certs (intermediate + root CAs), or <c>null</c> if no chain certs are present.
        /// </summary>
        private static byte[]? ExtractChainDerBytes(string pemString)
        {
            const string beginMarker = "-----BEGIN CERTIFICATE-----";
            const string endMarker = "-----END CERTIFICATE-----";

            var chainDerBlobs = new List<byte[]>();
            int searchPos = 0;
            int certIndex = 0;

            while (true)
            {
                int beginIdx = pemString.IndexOf(beginMarker, searchPos, StringComparison.Ordinal);
                if (beginIdx < 0) break;

                int endIdx = pemString.IndexOf(endMarker, beginIdx, StringComparison.Ordinal);
                if (endIdx < 0) break;

                int contentStart = beginIdx + beginMarker.Length;
                string b64 = pemString
                    .Substring(contentStart, endIdx - contentStart)
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Trim();

                // Skip index 0 — that is the leaf cert, already sent as deviceCertB64.
                if (certIndex > 0 && b64.Length > 0)
                    chainDerBlobs.Add(Convert.FromBase64String(b64));

                certIndex++;
                searchPos = endIdx + endMarker.Length;
            }

            if (chainDerBlobs.Count == 0) return null;

            int totalLen = chainDerBlobs.Sum(b => b.Length);
            byte[] concat = new byte[totalLen];
            int offset = 0;
            foreach (byte[] blob in chainDerBlobs)
            {
                Buffer.BlockCopy(blob, 0, concat, offset, blob.Length);
                offset += blob.Length;
            }
            return concat;
        }

        /// <summary>
        /// Concatenates DER bytes for ALL certificate blocks in <paramref name="pemString"/>.
        /// Use this when the PEM contains only CA/trust-anchor certs and there is no device
        /// leaf cert to skip (contrast with <see cref="ExtractChainDerBytes"/> which skips index 0).
        /// </summary>
        private static byte[]? ExtractAllCertsDerBytes(string pemString)
        {
            const string beginMarker = "-----BEGIN CERTIFICATE-----";
            const string endMarker = "-----END CERTIFICATE-----";

            var derBlobs = new List<byte[]>();
            int searchPos = 0;

            while (true)
            {
                int beginIdx = pemString.IndexOf(beginMarker, searchPos, StringComparison.Ordinal);
                if (beginIdx < 0) break;

                int endIdx = pemString.IndexOf(endMarker, beginIdx, StringComparison.Ordinal);
                if (endIdx < 0) break;

                int contentStart = beginIdx + beginMarker.Length;
                string b64 = pemString
                    .Substring(contentStart, endIdx - contentStart)
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Trim();

                if (b64.Length > 0)
                    derBlobs.Add(Convert.FromBase64String(b64));

                searchPos = endIdx + endMarker.Length;
            }

            if (derBlobs.Count == 0) return null;

            int totalLen = derBlobs.Sum(b => b.Length);
            byte[] concat = new byte[totalLen];
            int offset = 0;
            foreach (byte[] blob in derBlobs)
            {
                Buffer.BlockCopy(blob, 0, concat, offset, blob.Length);
                offset += blob.Length;
            }
            return concat;
        }

        public static GetProvisionStatusResponse DecodeGetProvisionStatus(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 1)
                throw new InvalidOperationException($"GetProvisionStatus payload too short: {payload.Length}");

            int o = 0;

            var status = (RpcStatus)payload[o++];

            if (status != RpcStatus.Ok)
                return new GetProvisionStatusResponse(status, null, null, null, null);

            if (payload.Length < o + 3)
                throw new InvalidOperationException("GetProvisionStatus missing required fields.");

            byte provisioningState = payload[o++];
            bool hasDeviceKey = payload[o++] != 0;
            bool hasDeviceCert = payload[o++] != 0;

            ushort? deviceCertLen = null;

            if (hasDeviceCert)
            {
                if (payload.Length < o + 2)
                    throw new InvalidOperationException("GetProvisionStatus missing deviceCertLen.");

                deviceCertLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(o, 2));
                o += 2;
            }

            return new GetProvisionStatusResponse(
                status,
                provisioningState,
                hasDeviceKey,
                hasDeviceCert,
                deviceCertLen);
        }

        public static GenerateCsrResponse DecodeGenerateCsr(ReadOnlySpan<byte> payload)
        {
            // Minimum length: 1 (status) + 32 (deviceId) + 2 (csrLen) = 35 bytes
            if (payload.Length < 35)
                throw new InvalidOperationException($"GenerateCSR payload too short: {payload.Length}");

            int o = 0;

            // 0: status (uint8_t)
            var status = (RpcStatus)payload[o++];

            if (status != RpcStatus.Ok)
                return new GenerateCsrResponse(status, null, 0, null);

            // 1: deviceId (string, fixed 32 chars per schema maxLength)
            // We convert the bytes directly to a UTF8 string
            string deviceId = Encoding.UTF8.GetString(payload.Slice(o, 32));
            o += 32;

            // Store it globally for your signing API DTO
            DeviceId = deviceId;

            // 2: csrDerLen (uint16_t)
            ushort csrLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(o, 2));
            o += 2;

            // 3: csrDer (base64/bytes)
            if (payload.Length < o + csrLen)
                throw new InvalidOperationException("CSR length exceeds payload.");

            Csr = payload.Slice(o, csrLen).ToArray();

            return new GenerateCsrResponse(status, deviceId, csrLen, Csr);
        }

        public static InstallCertificateResponse DecodeInstallCertificate(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 1)
                throw new InvalidOperationException($"InstallCertificate payload too short: {payload.Length}");

            return new InstallCertificateResponse((RpcStatus)payload[0]);
        }

        public static GetCertificateInfoResponse DecodeGetCertificateInfo(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 1)
                throw new InvalidOperationException($"GetCertificateInfo payload too short: {payload.Length}");

            int o = 0;

            var status = (RpcStatus)payload[o++];

            if (status != RpcStatus.Ok)
                return new GetCertificateInfoResponse(status, null, null);

            if (payload.Length < o + 2)
                throw new InvalidOperationException("GetCertificateInfo missing certLen.");

            ushort certLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(o, 2));
            o += 2;

            if (payload.Length < o + certLen)
                throw new InvalidOperationException("Certificate length exceeds payload.");

            byte[] cert = payload.Slice(o, certLen).ToArray();

            return new GetCertificateInfoResponse(status, certLen, cert);
        }

        /// <summary>
        /// Decodes the response from the SHP after an "Install Trust Policy" command.
        /// Expected payload: [status (1 byte)]
        /// </summary>
        public static InstallTrustPolicyResponse DecodeInstallTrustPolicy(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 1)
                throw new InvalidOperationException($"InstallTrustPolicy response too short: {payload.Length}");

            // The SHP returns a single status byte (0 = Success)
            var status = (RpcStatus)payload[0];

            return new InstallTrustPolicyResponse(status);
        }

        public static GetProvisionStatusResponse DecodeGetCommissioningStatus(ReadOnlySpan<byte> payload)
        {
            return DecodeGetProvisionStatus(payload);
        }

        public static GenerateCsrResponse DecodeGenerateCommissioningCsr(ReadOnlySpan<byte> payload)
        {
            // Minimum length: 1 (status) + 32 (deviceId) + 2 (csrLen) = 35 bytes
            if (payload.Length < 35)
                throw new InvalidOperationException($"GenerateCommissioningCSR payload too short: {payload.Length}");

            int o = 0;

            var status = (RpcStatus)payload[o++];

            if (status != RpcStatus.Ok)
                return new GenerateCsrResponse(status, null, 0, null);

            string deviceId = Encoding.UTF8.GetString(payload.Slice(o, 32));
            o += 32;

            CommissioningDeviceId = deviceId;

            ushort csrLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(o, 2));
            o += 2;

            if (payload.Length < o + csrLen)
                throw new InvalidOperationException("Commissioning CSR length exceeds payload.");

            CommissioningCsr = payload.Slice(o, csrLen).ToArray();

            return new GenerateCsrResponse(status, deviceId, csrLen, CommissioningCsr);
        }

        public static InstallCertificateResponse DecodeInstallCommissioningCertificate(ReadOnlySpan<byte> payload)
        {
            return DecodeInstallCertificate(payload);
        }

        public static GetCertificateInfoResponse DecodeGetCommissioningCertificateInfo(ReadOnlySpan<byte> payload)
        {
            return DecodeGetCertificateInfo(payload);
        }

        public static GetProvisionStatusResponse DecodeGetServerWifiStatus(ReadOnlySpan<byte> payload)
        {
            return DecodeGetProvisionStatus(payload);
        }

        public static GenerateCsrResponse DecodeGenerateServerWifiCsr(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 35)
                throw new InvalidOperationException($"GenerateServerWifiCSR payload too short: {payload.Length}");

            int o = 0;

            var status = (RpcStatus)payload[o++];

            if (status != RpcStatus.Ok)
                return new GenerateCsrResponse(status, null, 0, null);

            string deviceId = Encoding.UTF8.GetString(payload.Slice(o, 32));
            o += 32;

            ServerWifiDeviceId = deviceId;

            ushort csrLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(o, 2));
            o += 2;

            if (payload.Length < o + csrLen)
                throw new InvalidOperationException("Server WiFi CSR length exceeds payload.");

            ServerWifiCsr = payload.Slice(o, csrLen).ToArray();

            return new GenerateCsrResponse(status, deviceId, csrLen, ServerWifiCsr);
        }

        public static InstallCertificateResponse DecodeInstallServerWifiCertificate(ReadOnlySpan<byte> payload)
        {
            return DecodeInstallCertificate(payload);
        }

        public static GetCertificateInfoResponse DecodeGetServerWifiCertificateInfo(ReadOnlySpan<byte> payload)
        {
            return DecodeGetCertificateInfo(payload);
        }

        /// <summary>
        /// Decodes the Install Trust Chain response (same wire layout as Install Certificate: status byte only).
        /// </summary>
        public static InstallCertificateResponse DecodeInstallTrustChain(ReadOnlySpan<byte> payload)
            => DecodeInstallCertificate(payload);

        /// <summary>
        /// Decodes the Get Trust Chain Info response (same wire layout as Get Certificate Info:
        /// status + trustChainLen (uint16) + trustChainDer bytes).
        /// </summary>
        public static GetCertificateInfoResponse DecodeGetTrustChainInfo(ReadOnlySpan<byte> payload)
            => DecodeGetCertificateInfo(payload);

        public static InstallSetupCodeResponse DecodeInstallSetupCode(byte[] payload)
        {
            if (payload is null || payload.Length < 1)
                throw new ArgumentException("Payload too short.");

            var status = (RpcStatus)payload[0];

            var deviceId = payload.Length > 1
                ? Encoding.ASCII.GetString(payload, 1, payload.Length - 1).TrimEnd('\0')
                : string.Empty;

            return new InstallSetupCodeResponse(status, deviceId);
        }
    }
}