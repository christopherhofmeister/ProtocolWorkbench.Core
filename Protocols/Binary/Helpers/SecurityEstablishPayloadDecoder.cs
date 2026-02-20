using ProtocolWorkbench.Core.Protocols.Binary.Models;
using System.Diagnostics;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class SecurityEstablishPayloadDecoder
    {
        public static SecurityEstablishResponse Decode(ReadOnlySpan<byte> payload)
        {
            int o = 0;

            byte status = ReadU8(payload, ref o, "status");
            Log(payload, o, $"status={status}");

            // IMPORTANT: error responses are just status(u8)
            if (status != 0)
            {
                if (o != payload.Length)
                    Debug.WriteLine($"[SECDEC] NOTE: status={status}, trailing={payload.Length - o} bytes");
                return new SecurityEstablishResponse(status, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>());
            }

            ushort certLen = ReadU16LE(payload, ref o, "certLen");
            Log(payload, o, $"certLen={certLen}");

            if (certLen == 0)
                throw new FormatException("certLen=0 (invalid)");
            if (certLen > payload.Length - o)
                throw new FormatException($"certLen={certLen} exceeds remaining={payload.Length - o}");

            byte[] cert = ReadBytes(payload, ref o, certLen, "cert");
            Log(payload, o, $"cert read ({cert.Length} bytes)");

            ushort ephLen = ReadU16LE(payload, ref o, "ecdhPubLen");
            Log(payload, o, $"ecdhPubLen={ephLen}");

            if (ephLen > payload.Length - o)
                throw new FormatException($"ecdhPubLen={ephLen} exceeds remaining={payload.Length - o}");

            byte[] eph = ReadBytes(payload, ref o, ephLen, "ecdhPub");
            Log(payload, o, $"ecdhPub read ({eph.Length} bytes)");

            ushort sigLen = ReadU16LE(payload, ref o, "sigLen");
            Log(payload, o, $"sigLen={sigLen}");

            if (sigLen > payload.Length - o)
                throw new FormatException($"sigLen={sigLen} exceeds remaining={payload.Length - o}");

            byte[] sig = ReadBytes(payload, ref o, sigLen, "sig");
            Log(payload, o, $"sig read ({sig.Length} bytes)");

            if (o != payload.Length)
                throw new FormatException($"Trailing bytes: consumed={o} total={payload.Length} trailing={payload.Length - o}");

            return new SecurityEstablishResponse(status, cert, eph, sig);
        }

        private static void Log(ReadOnlySpan<byte> payload, int o, string msg)
            => Debug.WriteLine($"[SECDEC] {msg} o={o}/{payload.Length} remaining={payload.Length - o}");

        private static byte ReadU8(ReadOnlySpan<byte> s, ref int o, string fieldName)
        {
            if (o + 1 > s.Length)
                throw new FormatException($"Truncated u8 '{fieldName}' at o={o} len={s.Length}");
            return s[o++];
        }

        private static ushort ReadU16LE(ReadOnlySpan<byte> s, ref int o, string fieldName)
        {
            if (o + 2 > s.Length)
                throw new FormatException($"Truncated u16 '{fieldName}' at o={o} len={s.Length}");

            ushort v = (ushort)(s[o] | (s[o + 1] << 8));
            o += 2;
            return v;
        }

        private static byte[] ReadBytes(ReadOnlySpan<byte> s, ref int o, int len, string fieldName)
        {
            if (len < 0)
                throw new FormatException($"Negative len for '{fieldName}': {len}");

            if (o + len > s.Length)
                throw new FormatException($"Truncated bytes '{fieldName}' len={len} at o={o} remaining={s.Length - o} total={s.Length}");

            var b = s.Slice(o, len).ToArray();
            o += len;
            return b;
        }
    }
}