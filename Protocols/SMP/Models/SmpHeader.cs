namespace ProtocolWorkBench.Core.Models
{
    public class SmpHeader
    {
        public byte MgmtOp { get; set; }
        public byte Flags { get; set; }
        public UInt16HbLb PayloadLength { get; set; }
        public UInt16HbLb GroupId { get; set; }
        public byte SequenceNumber { get; set; }
        public byte MessageId { get; set; }

        public SmpHeader()
        {
            PayloadLength = new UInt16HbLb();
            GroupId = new UInt16HbLb();
        }
    }
}
