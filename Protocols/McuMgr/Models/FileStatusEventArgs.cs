using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class FileStatusEventArgs
    {
        public string PortName;
        public UInt64 FileLength;

        public FileStatusEventArgs()
        {
            PortName = null;
            FileLength = 0;
        }

        public FileStatusEventArgs(string portName, UInt64 fileLength)
        {
            PortName = portName;
            FileLength = fileLength;
        }
    }
}
