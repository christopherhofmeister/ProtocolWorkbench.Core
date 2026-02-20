using System.Security.Cryptography;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class BuildSecureFrameHelper
    {
        private static byte[] BuildSecureFrame_ChaCha20Poly1305(
            ushort typeValue,
            byte flags,
            uint seq,
            byte[] plaintextPayload,
            byte[] key32,
            byte[] nonceBase12)
        {
            const byte SOF = 0xAA;
            const byte EOF = 0x55;
            const int TagSize = 16;

            if (key32.Length != 32) throw new ArgumentException("Key must be 32 bytes.", nameof(key32));
            if (nonceBase12.Length != 12) throw new ArgumentException("Nonce base must be 12 bytes.", nameof(nonceBase12));

            // nonce = nonceBase (12) with last 4 bytes overwritten by SEQ (little-endian)
            byte[] nonce = (byte[])nonceBase12.Clone();
            nonce[8] = (byte)(seq & 0xFF);
            nonce[9] = (byte)((seq >> 8) & 0xFF);
            nonce[10] = (byte)((seq >> 16) & 0xFF);
            nonce[11] = (byte)((seq >> 24) & 0xFF);

            // LEN = bytes AFTER LEN field: TYPE(2)+FLAGS(1)+SEQ(4)+PAYLOAD+TAG(16)+EOF(1)
            ushort lenAfterLen = checked((ushort)(2 + 1 + 4 + plaintextPayload.Length + TagSize + 1));

            // AAD = LEN + TYPE + FLAGS + SEQ (little-endian)
            byte[] aad = new byte[2 + 2 + 1 + 4];
            int a = 0;
            aad[a++] = (byte)(lenAfterLen & 0xFF);
            aad[a++] = (byte)((lenAfterLen >> 8) & 0xFF);
            aad[a++] = (byte)(typeValue & 0xFF);
            aad[a++] = (byte)((typeValue >> 8) & 0xFF);
            aad[a++] = flags;
            aad[a++] = (byte)(seq & 0xFF);
            aad[a++] = (byte)((seq >> 8) & 0xFF);
            aad[a++] = (byte)((seq >> 16) & 0xFF);
            aad[a++] = (byte)((seq >> 24) & 0xFF);

            byte[] ciphertext = new byte[plaintextPayload.Length];
            byte[] tag = new byte[TagSize];

            using var aead = new ChaCha20Poly1305(key32);
            aead.Encrypt(nonce, plaintextPayload, ciphertext, tag, aad);

            // Frame = SOF + (LEN/TYPE/FLAGS/SEQ) + CIPHERTEXT + TAG + EOF
            byte[] frame = new byte[1 + aad.Length + ciphertext.Length + tag.Length + 1];
            int o = 0;

            frame[o++] = SOF;

            // header (LEN..SEQ) is the *same bytes* as AAD
            Buffer.BlockCopy(aad, 0, frame, o, aad.Length);
            o += aad.Length;

            if (ciphertext.Length > 0)
            {
                Buffer.BlockCopy(ciphertext, 0, frame, o, ciphertext.Length);
                o += ciphertext.Length;
            }

            Buffer.BlockCopy(tag, 0, frame, o, tag.Length);
            o += tag.Length;

            frame[o++] = EOF;

            return frame;
        }
    }
}
