using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkBench.Core.Models
{
    public class MessageParameter : KeyValuePair
    {
        public CTypes? CType { get; set; }
        public byte[] ByteArray { get; set; }

        public MessageParameter()
        {
            ByteArray = Array.Empty<byte>();
        }
    }
}
