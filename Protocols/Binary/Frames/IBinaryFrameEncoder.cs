namespace ProtocolWorkbench.Core.Protocols.Binary.Frames
{
    public interface IBinaryFrameEncoder
    {
        byte[] Encode(BinaryFrame frame);
        byte[] EncodeSecureChaCha20Poly1305(BinaryFrame frame, byte[] key32, byte[] nonceBase12);
    }
}