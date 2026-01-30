namespace ProtocolWorkbench.Core.Services.SerialPortService
{
    public sealed class SerialPortService : ISerialPortService
    {
        public IReadOnlyList<string> GetPortNames()
            => System.IO.Ports.SerialPort.GetPortNames()
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
