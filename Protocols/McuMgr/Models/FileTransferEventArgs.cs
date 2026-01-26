using System;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class FileTransferEventArgs
    {
        public string PortName;
        public UInt64 Offset;
        public UInt64 ExpectedFileSizeBytes;

        public FileTransferEventArgs()
        {
            PortName = null;
            Offset = 0;
            ExpectedFileSizeBytes = 0;
        }

        public FileTransferEventArgs(string portName, UInt64 offset, UInt64 expectedFileSizeBytes)
        {
            PortName = portName;
            Offset = offset;
            ExpectedFileSizeBytes = expectedFileSizeBytes;
        }
    }
}
