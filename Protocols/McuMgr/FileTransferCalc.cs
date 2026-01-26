using System;
using System.Collections.Generic;
using System.Text;

namespace ProtocolWorkBench.Core.Protocols.McuMgr
{
    public static class FileTransferCalc
    {
        // these values were measured on a saleae logic analyzer.
        public const int UploadTransferSpeedBytesPerSecond = 790;
        public const int DownloadTransferSpeedBytesPerSecond = 2375;
        private const int SecondsMuliplier = 4;
        private const int MilliSecPerSec = 1000;

        public static int CalculateFileUploadTimeoutMs(int FileSizeBytes)
        {
            int transferSeconds = FileSizeBytes / UploadTransferSpeedBytesPerSecond;
            if (transferSeconds == 0)
            {
                return SecondsMuliplier * MilliSecPerSec;
            }
            else
            {
                return transferSeconds * SecondsMuliplier * MilliSecPerSec;
            }
        }

        public static int CalculateFileDownloadTimeoutMs(int FileSizeBytes)
        {
            int transferSeconds = FileSizeBytes / DownloadTransferSpeedBytesPerSecond;
            if (transferSeconds == 0)
            {
                return SecondsMuliplier * MilliSecPerSec;
            }
            else
            {
                return transferSeconds * SecondsMuliplier * MilliSecPerSec;
            }
        }
    }
}
