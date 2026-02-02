using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Client
{
    public interface IBinaryMessageBuilder
    {
        List<byte> CreateBinaryMessage(UInt16HbLb id, IReadOnlyList<MessageParameter> msgParams);
    }
}