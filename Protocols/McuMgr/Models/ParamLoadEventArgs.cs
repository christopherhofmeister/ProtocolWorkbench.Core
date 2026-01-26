using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class ParamLoadEventArgs
    {
        public string PortName;
        public SmpHeader SmpHeader;
        public int Result;
        public string FilePathErrors;

        public ParamLoadEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            Result = -1;
            FilePathErrors = null;
        }

        public ParamLoadEventArgs(string portName, SmpHeader smpHeader, int result, string filePathErrors)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            Result = result;
            FilePathErrors = filePathErrors;
        }
    }
}
