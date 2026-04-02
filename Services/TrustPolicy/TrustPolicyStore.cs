using Microsoft.Maui.Storage;

namespace ProtocolWorkbench.Core.Services.TrustPolicy
{

    public class SecureTrustPolicyStore : ISecureTrustPolicyStore
    {
        private const string PolicyKey = "shp_active_trust_policy";

        public async Task SavePolicyAsync(byte[] raw100Bytes)
        {
            if (raw100Bytes == null || raw100Bytes.Length != 100)
                throw new ArgumentException("Trust Policy must be exactly 100 bytes.", nameof(raw100Bytes));

            // Convert to Base64 for string storage in SecureStorage
            string b64 = Convert.ToBase64String(raw100Bytes);
            await SecureStorage.Default.SetAsync(PolicyKey, b64);
        }

        public async Task<TrustPolicy?> GetCurrentPolicyAsync()
        {
            try
            {
                string? b64 = await SecureStorage.Default.GetAsync(PolicyKey);
                if (string.IsNullOrEmpty(b64))
                    return null;

                byte[] raw = Convert.FromBase64String(b64);
                if (raw.Length != 100) return null;

                // Unpack the binary bundle
                using var ms = new MemoryStream(raw);
                using var reader = new BinaryReader(ms);

                return new TrustPolicy
                {
                    Version = reader.ReadUInt32(),                   // 4 bytes LE
                    AllowedIntermediateSpkiHash = reader.ReadBytes(32), // 32 bytes
                    RootSignature = reader.ReadBytes(64),            // 64 bytes
                    Raw = raw
                };
            }
            catch (Exception ex)
            {
                // Log decryption or parsing errors
                System.Diagnostics.Debug.WriteLine($"TrustStore Error: {ex.Message}");
                return null;
            }
        }
    }
}
