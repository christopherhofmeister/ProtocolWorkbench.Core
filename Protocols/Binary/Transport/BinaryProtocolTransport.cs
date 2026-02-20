using ProtocolWorkbench.Core.Protocols.Binary.Frames;
using ProtocolWorkbench.Core.Services.SerialPortService;
using System.Diagnostics;

namespace ProtocolWorkbench.Core.Protocols.Binary.Transport
{
    public sealed class BinaryProtocolTransport : IBinaryProtocolTransport, IDisposable
    {
        private readonly ISerialTransport _serial;
        private readonly IBinaryFrameDecoder _decoder;
        private readonly IBinaryFrameEncoder _encoder;

        public event Action<BinaryFrame>? FrameReceived;
        public event Action<string>? ProtocolError;

        public event Action<byte[]>? FrameTransmittedBytes; // ->
        public event Action<byte[]>? FrameReceivedBytes;    // <-

        public BinaryProtocolTransport(
            ISerialTransport serial,
            IBinaryFrameDecoder decoder,
            IBinaryFrameEncoder encoder)
        {
            _serial = serial;
            _decoder = decoder;
            _encoder = encoder;

            _serial.ByteReceived += OnByteReceived;

            _decoder.FrameDecoded += OnFrameDecoded;

            _decoder.FrameError += e => ProtocolError?.Invoke(e);
        }

        private void OnByteReceived(byte b)
            => _decoder.PushByte(b);

        public void Send(BinaryFrame frame)
        {
            var bytes = _encoder.Encode(frame);

            // log BEFORE write so you still see it if write throws
            FrameTransmittedBytes?.Invoke(bytes);

            _serial.Write(bytes);
        }

        public void SendRaw(byte[] bytes)
        {
            FrameTransmittedBytes?.Invoke(bytes);

            _serial.Write(bytes);
        }

        private void OnFrameDecoded(BinaryFrame f)
        {
            try
            {
                FrameReceived?.Invoke(f);
                var rxBytes = _encoder.Encode(f);
                FrameReceivedBytes?.Invoke(rxBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            _serial.ByteReceived -= OnByteReceived;
        }
    }
}