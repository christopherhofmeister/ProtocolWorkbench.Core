using ProtocolWorkbench.Core.Protocols.Binary.Frames;

namespace ProtocolWorkbench.Core.Protocols.Binary.Transport
{
    public interface IBinaryProtocolTransport
    {
        event Action<BinaryFrame>? FrameReceived;
        event Action<string>? ProtocolError;

        event Action<byte[]>? FrameTransmittedBytes; // -> raw bytes written
        event Action<byte[]>? FrameReceivedBytes;    // <- raw bytes (best-effort for now)

        void Dispose();
        void Send(BinaryFrame frame);
    }
}