
namespace ProtocolWorkbench.Core.Protocols.Binary.Client
{
    public interface IJobIdAllocator
    {
        ushort Next(HashSet<ushort> inFlight);
    }
}