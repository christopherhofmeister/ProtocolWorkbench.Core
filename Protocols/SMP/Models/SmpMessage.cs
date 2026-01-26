using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolWorkBench.Core.Protocols.SMP.Models
{
    public class SmpMessage
    {
        public SmpHeader Header { get; set; }
        public List<byte> CBorMessage { get; set; }

        public SmpMessage()
        {
            Header = new SmpHeader();
            CBorMessage = new List<byte>();
        }

    }
}
