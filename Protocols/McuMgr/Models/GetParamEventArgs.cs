using ProtocolWorkBench.Core.Models;
using System;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class GetParamEventArgs
    {
        public string PortName;
        public SmpHeader SmpHeader;
        public int Result;
        public UInt16 Id;
        public object Value;

        public GetParamEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            Result = -1;
            Id = 0;
            Value = new object();
        }

        public GetParamEventArgs (string portName, SmpHeader smpHeader, int result, UInt16 id, object value)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            Result = result;
            Id = id;
            Value = value;
        }
    }
}
