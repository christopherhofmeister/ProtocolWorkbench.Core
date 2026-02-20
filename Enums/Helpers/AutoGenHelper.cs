namespace ProtocolWorkbench.Core.Enums.Helpers
{
    public static class AutoGenHelper
    {
        // These tokens are what you store in OpenRPC JSON:  "x-autogen": "<token>"
        public const string RANDOM16 = "random16";
        public const string P256_EPHEMERAL_PUBLIC = "p256-ephemeral-public";
        public const string SP_NONCE16_B64 = "sp-nonce16-b64";

        public static bool TryParse(string? token, out AutoGenKind kind)
        {
            kind = AutoGenKind.None;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            switch (token.Trim().ToLowerInvariant())
            {
                case RANDOM16:
                    kind = AutoGenKind.Random16B64;
                    return true;

                case P256_EPHEMERAL_PUBLIC:
                    kind = AutoGenKind.P256EphemeralPublicB64;
                    return true;

                case SP_NONCE16_B64:
                    kind = AutoGenKind.SPNonce16B64;
                    return true;

                default:
                    return false;
            }
        }

        public static string ToToken(AutoGenKind kind) =>
            kind switch
            {
                AutoGenKind.Random16B64 => RANDOM16,
                AutoGenKind.P256EphemeralPublicB64 => P256_EPHEMERAL_PUBLIC,
                _ => string.Empty
            };

        // Optional: for your Param editor placeholder / picker list
        public static IReadOnlyList<string> AllTokens { get; } = new[]
        {
            RANDOM16,
            P256_EPHEMERAL_PUBLIC
        };
    }
}
