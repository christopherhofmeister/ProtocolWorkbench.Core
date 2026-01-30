
namespace ProtocolWorkbench.Core.Services.SerialPortService
{
    public interface ISerialPortService
    {
        IReadOnlyList<string> GetPortNames();
    }
}