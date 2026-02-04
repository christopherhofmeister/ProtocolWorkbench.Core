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
            _readerTask = Task.Run(() => ReaderLoopAsync(_cts.Token));
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

        private async Task ReaderLoopAsync(CancellationToken token)
        {
            var port = _port;
            if (port is null) return;

            var stream = port.BaseStream;
            var buf = new byte[256];

            while (!token.IsCancellationRequested && port.IsOpen)
            {
                int n;
                try
                {
                    n = await stream.ReadAsync(buf, 0, buf.Length, token);
                }
                catch { break; }

                for (int i = 0; i < n; i++)
                    ByteReceived?.Invoke(buf[i]);
            }
        }
    }
}
