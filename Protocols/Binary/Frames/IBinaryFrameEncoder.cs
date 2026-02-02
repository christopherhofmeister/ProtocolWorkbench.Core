namespace ProtocolWorkbench.Core.Protocols.Binary.Frames
{
    public interface IBinaryFrameEncoder
    {
        byte[] Encode(BinaryFrame frame);
    }
}