using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Services.CrcService
{
    public interface ICrcService
    {
        public UInt16HbLb ComputeCcitt16(ReadOnlySpan<byte> data, ushort initial = 0xFFFF);
    }
}