using System.IO.Ports;

namespace ProtocolWorkbench.Core.Services.SerialPortService
{
    public sealed class SerialPortService : ISerialTransport
    {
        private SerialPort? _port;
        private CancellationTokenSource? _cts;
        private Task? _readerTask;

        public event Action<byte>? ByteReceived;

        public bool IsOpen => _port?.IsOpen == true;

        public int IdleGapMs { get; set; } = 30;

        public IReadOnlyList<string> GetPortNames()
            => SerialPort.GetPortNames()
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

        public void Open(string portName, int baudRate, bool hardwareFlowControl)
        {
            Close();

            _port = new SerialPort(portName, baudRate)
            {
                Handshake = hardwareFlowControl ? Handshake.RequestToSend : Handshake.None,
                ReadTimeout = IdleGapMs, // key: lets “idle gap” style logic work
                WriteTimeout = 500
            };

            _port.Open();

            _cts = new CancellationTokenSource();
            _readerTask = Task.Run(() => ReaderLoop(_cts.Token));
        }

        public void Close()
        {
            try { _cts?.Cancel(); } catch { }

            try { _port?.Close(); } catch { }

            _cts = null;
            _readerTask = null;
            _port = null;
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            if (_port?.IsOpen != true)
                throw new InvalidOperationException("Serial port is not open.");

            // SerialPort requires byte[]
            var buf = data.ToArray();
            _port.Write(buf, 0, buf.Length);
        }

        private void ReaderLoop(CancellationToken token)
        {
            // single-byte event because you explicitly want Action<byte>
            // (later we can optimize to chunk events if needed)
            while (!token.IsCancellationRequested && _port?.IsOpen == true)
            {
                try
                {
                    int b = _port.ReadByte();     // blocks until byte or timeout
                    if (b >= 0)
                        ByteReceived?.Invoke((byte)b);
                }
                catch (TimeoutException)
                {
                    // This is your “idle gap” signal.
                    // For Phase 1, transport just ignores it.
                    // The *protocol framer* can decide what to do with gaps.
                }
                catch
                {
                    // port closed / error
                    break;
                }
            }
        }
    }
}
