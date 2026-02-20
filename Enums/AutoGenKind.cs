namespace ProtocolWorkbench.Core.Enums
{
    public enum AutoGenKind
    {
        None = 0,

        // base64 strings by default (since your OpenRPC examples are *B64 fields*)
        Random16B64,            // 16 random bytes -> base64
        P256EphemeralPublicB64, // SEC1 uncompressed pubkey -> base64
        SecurityKeyConfirmValueB64,
        SPNonce16B64,

        // optional future ones
        Random32B64,
        UuidString,
        UnixTimeSeconds
    }
}
