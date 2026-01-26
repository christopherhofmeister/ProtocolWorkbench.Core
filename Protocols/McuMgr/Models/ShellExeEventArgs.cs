using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class ShellExeEventArgs
    {
        public string? PortName;
        public SmpHeader SmpHeader;
        public int Rc;
        public string? Output;

        public ShellExeEventArgs()
        {
            PortName = null;
            SmpHeader = new SmpHeader();
            Rc = -1;
            Output = null;
        }

        public ShellExeEventArgs(string portName, SmpHeader smpHeader, int rc, string output)
        {
            PortName = portName;
            SmpHeader = smpHeader;
            Rc = rc;
            Output = output;
        }
    }
}
