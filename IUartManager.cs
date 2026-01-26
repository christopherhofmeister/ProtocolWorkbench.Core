using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core
{
    public interface IUartManager
    {
        List<FtdiDevice> GetComPorts(bool FTDIOnly, bool closeComPorts, int number = 0, int mockStartEnum = 0);
        void CloseComPorts();
        void CloseComPort(string comPort);
        List<IUartDevice> SerialPorts { get; set; }
        void ClearUserAttributes();
        (bool Success, IUartDevice Uart) GetUartByComPort(string comPort);
        bool DebugMode { get; set; }
        bool FtdiPortsResetAsyncBitBangMode();
        bool FtdiPortsSetAsyncBitBangMode();
    }
}
