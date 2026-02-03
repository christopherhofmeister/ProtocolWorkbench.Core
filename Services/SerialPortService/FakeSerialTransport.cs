namespace ProtocolWorkbench.Core.Services.SerialPortService
{
    public sealed class FakeSerialTransport : ISerialTransport
    {
        public event Action<byte>? ByteReceived;

        public bool IsOpen { get; private set; }

        public int IdleGapMs { get; set; } = 30;

        // configurable port list for tests
        private readonly List<string> _ports;

        // captures what was written
        private readonly List<byte> _tx = new();

        public FakeSerialTransport(IEnumerable<string>? ports = null)
        {
            _ports = ports?.ToList() ?? new List<string> { "COM1", "COM2", "COM3" };
        }

        public IReadOnlyList<string> GetPortNames() => _ports;

        public IReadOnlyList<byte> WrittenBytes => _tx;

        public void Open(string portName, int baudRate, bool hardwareFlowControl)
        {
            // Simple validation so tests catch mistakes
            if (string.IsNullOrWhiteSpace(portName))
                throw new ArgumentException("portName is required", nameof(portName));

            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            if (!IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            for (int i = 0; i < data.Length; i++)
                _tx.Add(data[i]);
        }

        // --- Helpers for driving RX in tests ---

        public void InjectRxByte(byte b) => ByteReceived?.Invoke(b);

        public void InjectRxBytes(ReadOnlySpan<byte> bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
                ByteReceived?.Invoke(bytes[i]);
        }

        public void ClearWritten() => _tx.Clear();
    }
}
