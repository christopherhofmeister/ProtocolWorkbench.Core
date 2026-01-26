using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Models
{
    public class UartFlowControl
    {
        public const string UART_FLOW_CONTROL_NONE = "None";
        public const string UART_FLOW_CONTROL_RTS = "RTS";
        public const string UART_FLOW_CONTROL_XOnXOff = "XOnXOff";
        public const string UART_FLOW_CONTROL_RTSXOnXoff = "RequestToSendXOnXOff";

        public enum FlowControlTypes
        {
            None,
            Rts,
            XonXoff,
            RtsXonXoff
        }
    }
}
