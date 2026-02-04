namespace ProtocolWorkBench.Core.Models
{
    public class UInt16HbLb
    {
        private ushort _u16;

        public byte Lb
        {
            get => (byte)(_u16 & 0xFF);
            set => _u16 = (ushort)((_u16 & 0xFF00) | value);
        }

        public byte Hb
        {
            get => (byte)((_u16 >> 8) & 0xFF);
            set => _u16 = (ushort)((_u16 & 0x00FF) | (value << 8));
        }

        public ushort U16Value
        {
            get => _u16;
            set => _u16 = value;
        }

        public UInt16HbLb() : this(0) { }
        public UInt16HbLb(ushort initValue) => _u16 = initValue;
        public UInt16HbLb(int initValue) => _u16 = Convert.ToUInt16(initValue);
    }
}