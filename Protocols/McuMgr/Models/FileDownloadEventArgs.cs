using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr.Models
{
    public class FileDownloadEventArgs
    {
        public string PortName;
        public string DownloadPath;

        public FileDownloadEventArgs()
        {
            PortName = null;
            DownloadPath = null;
        }

        public FileDownloadEventArgs(string portName, string downloadPath)
        {
            PortName = portName;
            DownloadPath = downloadPath;
        }
    }
}
