using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolWorkBench.Core.Protocols.Binary.Models
{
    /* Binary Protocol B 
         * NAME     SIZE      VALUE
         * START    BYTE        01
         * LENGTH   2 BYTES     Variable - LSB
         * TYPE     BYTE        0-127
         * PAYLOAD  VARIABLE    Variable
         * CRC16    2 BYTES     Variable - LSB (Sum of START through Payload)
         * END      BYTE        04
         * 
    */

    public class BinaryProtocolB
    {
        public byte StartOfFrame;
        public UInt16HbLb Length;
        public byte MesssageType;
        public List<byte> Payload;
        public UInt16HbLb CRC;
        public byte EndOfFrame;

        public BinaryProtocolB()
        {
            Payload = new List<byte>();
            Length = new UInt16HbLb();
            CRC = new UInt16HbLb();
        }
    }
}
