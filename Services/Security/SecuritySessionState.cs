using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProtocolWorkbench.Core.Services.Security
{
    public class SecuritySessionState : ISecuritySessionState
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        private const string FileName = "security-session.json";
        private string FilePath => Path.Combine(FileSystem.AppDataDirectory, FileName);

        // ===== Interface members =====

        public ECDiffieHellman? SpEcdh { get; set; }

        public string? SpPublicB64 { get; set; }

        public byte[]? SpNonce { get; set; }
        public string? SpNonceB64 => SpNonce is null ? null : Convert.ToBase64String(SpNonce);

        public SecuritySessionState()
        {
            //InstanceId=bb2d4de2-d1cf-4448-9262-55acf83fcef4 FilePath=C:\Users\christopherhofmeiste\AppData\Local\User Name\com.companyname.protocolworkbench\Data\security-session.json
            Debug.WriteLine($"SecuritySessionState InstanceId={InstanceId} FilePath={FilePath}");
        }

        public uint NextSecureTxSeq()
        {
            var v = SecureTxSeq;
            SecureTxSeq = checked(SecureTxSeq + 1);
            return v;
        }

        public async Task<byte[]> EnsureSpNonceAsync()
        {
            if (SpNonce is not null && SpNonce.Length == 16)
                return SpNonce;

            await LoadAsync();

            if (SpNonce is not null && SpNonce.Length == 16)
                return SpNonce;

            SpNonce = RandomNumberGenerator.GetBytes(16);
            await SaveAsync();
            return SpNonce;
        }

        public async Task<string> EnsureSpNonceB64Async()
        {
            byte[] n = await EnsureSpNonceAsync(); // generates + saves if needed
            return Convert.ToBase64String(n);
        }

        public async Task<string> EnsureSpEphemeralPublicB64Async()
        {
            Debug.WriteLine($"InstanceId = {InstanceId}");

            Debug.WriteLine("---- EnsureSpEphemeralPublicB64Async ENTER ----");
            Debug.WriteLine($"Before anything: SpEcdh null? {SpEcdh == null}");
            Debug.WriteLine($"Before anything: SpPublicB64 null/empty? {string.IsNullOrWhiteSpace(SpPublicB64)}");

            // 1) If already fully initialized in memory, return it
            if (SpEcdh != null && !string.IsNullOrWhiteSpace(SpPublicB64))
            {
                Debug.WriteLine("Reusing existing in-memory SP ECDH (no regeneration).");
                Debug.WriteLine("---- EnsureSpEphemeralPublicB64Async EXIT (reuse memory) ----");
                return SpPublicB64!;
            }

            Debug.WriteLine("Attempting LoadAsync()...");
            await LoadAsync();

            Debug.WriteLine($"After LoadAsync: SpEcdh null? {SpEcdh == null}");
            Debug.WriteLine($"After LoadAsync: SpPublicB64 null/empty? {string.IsNullOrWhiteSpace(SpPublicB64)}");

            if (SpEcdh != null && !string.IsNullOrWhiteSpace(SpPublicB64))
            {
                Debug.WriteLine("Reusing SP ECDH restored from disk.");
                Debug.WriteLine("---- EnsureSpEphemeralPublicB64Async EXIT (reuse disk) ----");
                return SpPublicB64!;
            }

            // 3) If we have a key but no public string, derive it (DO NOT regenerate)
            if (SpEcdh != null)
            {
                Debug.WriteLine("SpEcdh exists but SpPublicB64 missing — deriving SEC1 public.");
                SpPublicB64 = ExportSec1UncompressedPublicB64(SpEcdh);

                Debug.WriteLine($"Derived SP public (SEC1) = {SpPublicB64}");

                await SaveAsync();

                Debug.WriteLine("---- EnsureSpEphemeralPublicB64Async EXIT (derived public only) ----");
                return SpPublicB64!;
            }

            // 4) Generate once (true first-time creation only)
            Debug.WriteLine("Generating NEW SP ECDH keypair.");
            SpEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            SpPublicB64 = ExportSec1UncompressedPublicB64(SpEcdh);

            Debug.WriteLine($"NEW SP public (SEC1) = {SpPublicB64}");

            await SaveAsync();

            Debug.WriteLine("---- EnsureSpEphemeralPublicB64Async EXIT (generated new) ----");
            return SpPublicB64!;
        }

        public async Task LoadAsync()
        {
            if (!File.Exists(FilePath))
                return;

            var json = await File.ReadAllTextAsync(FilePath);
            var state = JsonSerializer.Deserialize<PersistedState>(json);

            if (state is null)
                return;

            // Restore SP private key (optional)
            if (!string.IsNullOrWhiteSpace(state.PrivateKeyPkcs8B64))
            {
                var privateBytes = Convert.FromBase64String(state.PrivateKeyPkcs8B64);

                SpEcdh?.Dispose();
                SpEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                SpEcdh.ImportPkcs8PrivateKey(privateBytes, out _);

                // ALWAYS derive SEC1 public from the private key we just loaded
                SpPublicB64 = ExportSec1UncompressedPublicB64(SpEcdh);
            }

            // Restore nonce (optional)
            SpNonce = EnsureLen(FromB64(state.SpNonceB64), 16);

            // Restore derived session material (optional)
            TranscriptHash = EnsureLen(FromB64(state.TranscriptHashB64), 32);

            KeySpToShp = EnsureLen(FromB64(state.KeySpToShpB64), 16);
            KeyShpToSp = EnsureLen(FromB64(state.KeyShpToSpB64), 16);
            NonceBaseSpToShp = EnsureLen(FromB64(state.NonceBaseSpToShpB64), 13);
            NonceBaseShpToSp = EnsureLen(FromB64(state.NonceBaseShpToSpB64), 13);

            SecureTxSeq = state.SecureTxSeq;
            SecureRxSeq = state.SecureRxSeq;

            Mode = Enum.TryParse(state.Mode, out SecurityMode m) ? m : SecurityMode.Plaintext;
        }

        public async Task SaveAsync()
        {
            // Persist even if SpEcdh is null (we still want to save derived session material)
            string? privateKeyB64 = null;
            if (SpEcdh is not null)
            {
                var privateBytes = SpEcdh.ExportPkcs8PrivateKey();
                privateKeyB64 = Convert.ToBase64String(privateBytes);
            }

            var state = new PersistedState
            {
                PrivateKeyPkcs8B64 = privateKeyB64,

                SpNonceB64 = ToB64(SpNonce),

                TranscriptHashB64 = ToB64(TranscriptHash),

                KeySpToShpB64 = ToB64(KeySpToShp),
                KeyShpToSpB64 = ToB64(KeyShpToSp),
                NonceBaseSpToShpB64 = ToB64(NonceBaseSpToShp),
                NonceBaseShpToSpB64 = ToB64(NonceBaseShpToSp),

                SecureTxSeq = SecureTxSeq,
                SecureRxSeq = SecureRxSeq,

                Mode = Mode.ToString()
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
        }

        public async Task ResetAsync()
        {
            SpEcdh?.Dispose();
            SpEcdh = null;
            SpPublicB64 = null;

            SpNonce = null;

            TranscriptHash = null;
            KeySpToShp = null;
            KeyShpToSp = null;
            NonceBaseSpToShp = null;
            NonceBaseShpToSp = null;

            SecureTxSeq = 0;
            SecureRxSeq = 0;

            Mode = SecurityMode.Plaintext;

            if (File.Exists(FilePath))
                File.Delete(FilePath);

            await Task.CompletedTask;
        }

        // ===== Additional session fields (not in interface, but needed for click-later flow) =====

        public byte[]? TranscriptHash { get; private set; }           // 32 bytes

        public byte[]? KeySpToShp { get; private set; }               // 16 bytes
        public byte[]? KeyShpToSp { get; private set; }               // 16 bytes
        public byte[]? NonceBaseSpToShp { get; private set; }         // 13 bytes
        public byte[]? NonceBaseShpToSp { get; private set; }         // 13 bytes

        public uint SecureTxSeq { get; private set; }                 // SP->SHP
        public uint SecureRxSeq { get; private set; }                 // SHP->SP

        public SecurityMode Mode { get; private set; } = SecurityMode.Plaintext;
        public enum SecurityMode { Plaintext, SecurePending, SecureEstablished }

        /// <summary>
        /// Call this AFTER Establish Session response is verified and keys are derived.
        /// This stores durable session material so the user can click Key Confirm later.
        /// </summary>
        public async Task SaveEstablishedSessionAsync(
            byte[] transcriptHash32,
            byte[] keySpToShp16,
            byte[] keyShpToSp16,
            byte[] nonceBaseSpToShp13,
            byte[] nonceBaseShpToSp13)
        {
            TranscriptHash = EnsureLenOrThrow(transcriptHash32, 32, nameof(transcriptHash32));
            KeySpToShp = EnsureLenOrThrow(keySpToShp16, 16, nameof(keySpToShp16));
            KeyShpToSp = EnsureLenOrThrow(keyShpToSp16, 16, nameof(keyShpToSp16));
            NonceBaseSpToShp = EnsureLenOrThrow(nonceBaseSpToShp13, 13, nameof(nonceBaseSpToShp13));
            NonceBaseShpToSp = EnsureLenOrThrow(nonceBaseShpToSp13, 13, nameof(nonceBaseShpToSp13));

            SecureTxSeq = 0;
            SecureRxSeq = 0;

            Mode = SecurityMode.SecurePending;

            await SaveAsync();
        }

        public async Task MarkSecureEstablishedAsync()
        {
            Mode = SecurityMode.SecureEstablished;
            await SaveAsync();
        }

        public async Task BumpSecureTxSeqAsync()
        {
            SecureTxSeq++;
            await SaveAsync();
        }

        public async Task BumpSecureRxSeqAsync()
        {
            SecureRxSeq++;
            await SaveAsync();
        }

        // ===== Persistence model =====

        private sealed class PersistedState
        {
            public string? PrivateKeyPkcs8B64 { get; set; }

            public string? SpNonceB64 { get; set; }

            public string? TranscriptHashB64 { get; set; }

            public string? KeySpToShpB64 { get; set; }
            public string? KeyShpToSpB64 { get; set; }
            public string? NonceBaseSpToShpB64 { get; set; }
            public string? NonceBaseShpToSpB64 { get; set; }

            public uint SecureTxSeq { get; set; }
            public uint SecureRxSeq { get; set; }

            public string? Mode { get; set; }
        }

        // ===== Key export helper =====

        private static string ExportSec1UncompressedPublicB64(ECDiffieHellman ecdh)
        {
            var pub = ecdh.ExportParameters(false);

            if (pub.Q.X is null || pub.Q.Y is null)
                throw new InvalidOperationException("ECDH public key missing X/Y.");

            // SEC1 uncompressed: 0x04 || X(32) || Y(32)
            var sec1 = new byte[65];
            sec1[0] = 0x04;
            Buffer.BlockCopy(pub.Q.X, 0, sec1, 1, 32);
            Buffer.BlockCopy(pub.Q.Y, 0, sec1, 33, 32);

            return Convert.ToBase64String(sec1);
        }

        // ===== Serialization helpers =====

        private static byte[]? FromB64(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : Convert.FromBase64String(s);

        private static string? ToB64(byte[]? b) =>
            b is null ? null : Convert.ToBase64String(b);

        private static byte[]? EnsureLen(byte[]? b, int len) =>
            b is not null && b.Length == len ? b : null;

        private static byte[] EnsureLenOrThrow(byte[] b, int len, string paramName)
        {
            if (b is null) throw new ArgumentNullException(paramName);
            if (b.Length != len) throw new ArgumentException($"Expected {len} bytes, got {b.Length}.", paramName);
            return b;
        }
    }
}