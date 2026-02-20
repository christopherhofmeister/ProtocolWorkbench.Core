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
