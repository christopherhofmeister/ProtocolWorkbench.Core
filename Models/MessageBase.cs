using System;
using System.Collections.Generic;

namespace ProtocolWorkBench.Core.Models
{
    public class MessageBase
    {
        public DateTime TimeStamp = new DateTime();
        public List<byte> FullMessage = new List<byte>();
        public string? ComPort { get; set; }
    }
}
