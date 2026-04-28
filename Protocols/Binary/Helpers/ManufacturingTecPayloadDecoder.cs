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