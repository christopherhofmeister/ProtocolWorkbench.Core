using ProtocolWorkbench.Core.Enums;
using System.Security.Cryptography;

namespace ProtocolWorkbench.Core.Services.AutoGenService
{

    public class AutoGenService : IAutoGenService
    {
        public string Generate(AutoGenKind kind)
        {
            return kind switch
            {
                AutoGenKind.Random16B64 => RandomBytesB64(16),

                // TODO: implement with your crypto stack (see notes below)
                AutoGenKind.P256EphemeralPublicB64 => throw new NotSupportedException(
                    "P-256 ephemeral public generation not implemented yet. Add ECC keygen and SEC1 encoding."),

                AutoGenKind.Random32B64 => RandomBytesB64(32),
                AutoGenKind.UuidString => Guid.NewGuid().ToString(),
                AutoGenKind.UnixTimeSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),

                _ => string.Empty
            };
        }

        private static string RandomBytesB64(int len)
        {
            var bytes = new byte[len];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
