using Microsoft.Maui.Storage;
using Shp.Device.Provisioning.Dtos.Enums;
using System.Diagnostics;

namespace ProtocolWorkbench.Core.Services.TrustPolicy
{
    public class SecureTrustPolicyStore : ISecureTrustPolicyStore
    {
        private static string GetPolicyKey(IntermediateCertificatePurpose purpose)
            => $"shp_active_trust_policy_{purpose}";

        public async Task SavePolicyAsync(IntermediateCertificatePurpose purpose, byte[] raw100Bytes)
        {
            if (raw100Bytes == null || raw100Bytes.Length != 100)
                throw new ArgumentException("Trust Policy must be exactly 100 bytes.", nameof(raw100Bytes));

            string b64 = Convert.ToBase64String(raw100Bytes);

            Debug.WriteLine($"[TRUST STORE SAVE] Purpose={purpose}");
            Debug.WriteLine($"[TRUST STORE SAVE] Key={GetPolicyKey(purpose)}");
            Debug.WriteLine($"[TRUST STORE SAVE] Version={BitConverter.ToUInt32(raw100Bytes, 0)}");
            Debug.WriteLine($"[TRUST STORE SAVE] SPKI={Convert.ToHexString(raw100Bytes.AsSpan(4, 32))}");

            await SecureStorage.Default.SetAsync(GetPolicyKey(purpose), b64);
        }

        public async Task<TrustPolicy?> GetCurrentPolicyAsync(IntermediateCertificatePurpose purpose)
        {
            try
            {
                string? b64 = await SecureStorage.Default.GetAsync(GetPolicyKey(purpose));
                if (string.IsNullOrEmpty(b64))
                    return null;

                byte[] raw = Convert.FromBase64String(b64);
                if (raw.Length != 100)
                    return null;

                Debug.WriteLine($"[TRUST STORE READ] Purpose={purpose}");
                Debug.WriteLine($"[TRUST STORE READ] Key={GetPolicyKey(purpose)}");
                Debug.WriteLine($"[TRUST STORE READ] Version={BitConverter.ToUInt32(raw, 0)}");
                Debug.WriteLine($"[TRUST STORE READ] SPKI={Convert.ToHexString(raw.AsSpan(4, 32))}");

                using var ms = new MemoryStream(raw);
                using var reader = new BinaryReader(ms);

                return new TrustPolicy
                {
                    Version = reader.ReadUInt32(),
                    AllowedIntermediateSpkiHash = reader.ReadBytes(32),
                    RootSignature = reader.ReadBytes(64),
                    Raw = raw
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TrustStore Error: {ex.Message}");
                return null;
            }
        }
    }
}