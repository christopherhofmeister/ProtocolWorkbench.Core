using System;

namespace ProtocolWorkBench.Core.Models
{
    public class MessageParameter : KeyValuePair
    {
        public string? CType { get; set; }
        public byte[] ByteArray { get; set; }

        public MessageParameter()
        {
            ByteArray = Array.Empty<byte>();
        }
    }
}
