using System.Collections.Generic;

namespace ProtocolWorkBench.Core.Models
{
    public static class StandardBaudRates
    {
        /* bits per second */
        public const int BAUD_9600 = 9600;
        public const int BAUD_14400 = 14400;
        public const int BAUD_19200 = 19200;
        public const int BAUD_38400 = 38400;
        public const int BAUD_57600 = 57600;
        public const int BAUD_115200 = 115200;
        public const int BAUD_230400 = 230400;
        public const int BAUD_460800 = 460800;
        public const int BAUD_921600 = 921600;

        public static List<int> BaudRates = new List<int> { BAUD_9600, BAUD_14400, BAUD_19200, BAUD_38400, 
            BAUD_57600, BAUD_115200, BAUD_230400, BAUD_460800, BAUD_921600 };
    }
}
