using ProtocolWorkBench.Core.Models;
using System;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class SetParamEventArgs
    {
        public string? PortName;
        public SmpHeader SmpHeader;
        public int Result;
        public UInt16 Id;

        public SetParamEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            Result = -1;
            Id = 0;
        }

        public SetParamEventArgs(string portName, SmpHeader smpHeader, int result, UInt16 id)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            Result = result;
            Id = id;
        }
    }
}
