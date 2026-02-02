namespace ProtocolWorkbench.Core.Services.SerialPortService
{
    public interface ISerialTransport
    {
        event Action<byte> ByteReceived;

        bool IsOpen { get; }

        IReadOnlyList<string> GetPortNames();

        void Open(string portName, int baudRate, bool hardwareFlowControl);
        void Close();

        void Write(ReadOnlySpan<byte> data);

        // for your text framing support
        int IdleGapMs { get; set; }
    }
}
