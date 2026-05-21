using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames
{
    public interface IBinaryFrameEncoder
    {
        byte[] Encode(BinaryFrame frame);
        byte[] EncodeSecureAes128Ccm(BinaryFrame frame, byte[] key16, byte[] nonceBase13);
        public List<Byte> ParameterToBytesLSBFirst(MessageParameter param);
    }
}