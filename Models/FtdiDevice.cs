using FTD2XX_NET;

namespace ProtocolWorkBench.Core.Models
{
    public class FtdiDevice
    {
        public string? ComPort { get; set; }
        public string? SerialNumber { get; set; }
        public FTDI.FT_DEVICE FtdiDeviceType { get; set; }
    }
}
