using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
   public class McuMgrParamsEventArgs
    {
        public string PortName;
        public SmpHeader SmpHeader;
        public UInt32 BufferSize;
        public UInt32 BufferCount;

        public McuMgrParamsEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            BufferSize = 0;
            BufferCount = 0;
        }

        public McuMgrParamsEventArgs(string portName, SmpHeader smpHeader,
            UInt32 bufferSize, UInt32 bufferCount)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            BufferSize = bufferSize;
            BufferCount = bufferCount;
        }
    }
}
