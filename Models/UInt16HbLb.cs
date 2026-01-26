using System;

namespace ProtocolWorkBench.Core.Models
{
    public class UInt16HbLb
    {
        private UInt16 u16Value;

        public byte Lb 
        { 
            get 
            { 
                return (byte)(u16Value & 0xff); 
            }
            set
            {
                u16Value += value;
            }
        }
        public byte Hb 
        { 
            get 
            { 
                return (byte)((u16Value & 0xff00) >> 8); 
            }
            set
            {
                u16Value += (UInt16)(value << 8);
            }
        }

        public UInt16 U16Value
        {
            get { return u16Value; }
            set { u16Value = value; }
        }

        public UInt16HbLb()
        {
            u16Value = new ushort();
        }

        public UInt16HbLb(UInt16 initValue)
        {
            u16Value = initValue;
        }
    }
}
