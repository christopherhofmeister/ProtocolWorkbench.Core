using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class FileUploadEventArgs
    {
        public string PortName;
        public UInt64 NumBytes;

        public FileUploadEventArgs()
        {
            PortName = null;
            NumBytes = 0;
        }

        public FileUploadEventArgs(string portName, UInt64 numBytes)
        {
            PortName = portName;
            NumBytes = numBytes;
        }
    }
}
