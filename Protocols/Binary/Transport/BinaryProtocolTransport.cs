using ProtocolWorkbench.Core.Protocols.Binary.Frames;
using ProtocolWorkbench.Core.Services.SerialPortService;

namespace ProtocolWorkbench.Core.Protocols.Binary.Transport
{
    public sealed class BinaryProtocolTransport : IBinaryProtocolTransport, IDisposable
    {
        private readonly ISerialTransport _serial;
        private readonly IBinaryFrameDecoder _decoder;
        private readonly IBinaryFrameEncoder _encoder;

        public event Action<BinaryFrame>? FrameReceived;
        public event Action<string>? ProtocolError;

        public BinaryProtocolTransport(
            ISerialTransport serial,
            IBinaryFrameDecoder decoder,
            IBinaryFrameEncoder encoder)
        {
            _serial = serial;
            _decoder = decoder;

            _serial.ByteReceived += OnByteReceived;
            _decoder.FrameDecoded += f => FrameReceived?.Invoke(f);
            _decoder.FrameError += e => ProtocolError?.Invoke(e);
            _encoder = encoder;
        }

        private void OnByteReceived(byte b)
            => _decoder.PushByte(b);

        public void Send(BinaryFrame frame)
        {
            var bytes = _encoder.Encode(frame);
            _serial.Write(bytes);
        }

        public void Dispose()
        {
            _serial.ByteReceived -= OnByteReceived;
        }
    }
}
