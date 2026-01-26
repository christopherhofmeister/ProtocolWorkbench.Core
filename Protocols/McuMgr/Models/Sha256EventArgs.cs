using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class Sha256EventArgs
    {
        public string? PortName;
        public string? Sha256Result;

        public Sha256EventArgs()
        {
            PortName = null;
            Sha256Result = null;
        }

        public Sha256EventArgs(string portName, string sha256Result)
        {
            PortName = portName;
            Sha256Result = sha256Result;
        }
    }
}
