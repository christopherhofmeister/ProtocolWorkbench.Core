using ProtocolWorkbench.Core.Protocols.Binary.Frames;

namespace ProtocolWorkbench.Core.Protocols.Binary.Transport
{
    public interface IBinaryProtocolTransport
    {
        event Action<BinaryFrame>? FrameReceived;
        event Action<string>? ProtocolError;

        void Dispose();
        void Send(BinaryFrame frame);
    }
}