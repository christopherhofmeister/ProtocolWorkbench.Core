using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkBench.Core.Protocols.Binary.Models
{
    /* Binary Protocol (11-byes of overhead + payload + auth)
        | Field | Size (bytes) | Description |
        |------|--------------|-------------|
        | SOF | 1 | Start-of-frame marker (0xAA) |
        | LEN | 2 | Payload length in bytes |
        | TYPE | 2 | Message type (category + message ID) |
        | FLAGS | 1 | Message semantics flags |
        | SEQ | 4 | Frame counter / sequence number |
        | PAYLOAD | LEN | Payload data (plaintext or encrypted) |
        | AUTH | N | CRC16 (plaintext mode) or AEAD tag (secure mode) |
        | EOF | 1 | End-of-frame marker (0x55) |
    */

    public class BinaryProtocol
    {
        public byte StartOfFrame;
        public UInt16HbLb Length;
        public UInt16HbLb MesssageType;
        public List<byte> Payload;
        public UInt16HbLb CRC;
        public byte EndOfFrame;

        public BinaryProtocol()
        {
            Payload = new List<byte>();
            Length = new UInt16HbLb();
            MesssageType = new UInt16HbLb();
            CRC = new UInt16HbLb();
        }
    }
}
