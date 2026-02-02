namespace ProtocolWorkbench.Core.Protocols.Binary.Frames
{
    public interface IBinaryFrameDecoder
    {
        event Action<BinaryFrame>? FrameDecoded;
        event Action<string>? FrameError;

        void PushByte(byte b);
        void Reset();
    }
}