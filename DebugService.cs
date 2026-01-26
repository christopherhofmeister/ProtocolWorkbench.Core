using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ProtocolWorkBench.Core
{
    public static class DebugService
    {
        public static bool OutputDebug {get; set;}

        public static void PrintDebug (string message, string comPort = null, string heading = null)
        {
            if (OutputDebug)
            {
                Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss:fff")} {comPort}: {heading} {message}");
            }
        }
    }
}
