using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class ParamDumpEventArgs
    {
        public string PortName;
        public SmpHeader SmpHeader;
        public int Result;
        public string Name;

        public ParamDumpEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            Result = -1;
            Name = null;
        }

        public ParamDumpEventArgs(string portName, SmpHeader smpHeader, int result, string name)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            Result = result;
            Name = name;
        }
    }
}
