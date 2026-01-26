using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class FactoryResetEventArgs
    {
        public string PortName;
        public SmpHeader SmpHeader;
        public int Result;

        public FactoryResetEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            Result = -1;
        }

        public FactoryResetEventArgs(string portName, SmpHeader smpHeader, int result)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            Result = result;
        }
    }
}
