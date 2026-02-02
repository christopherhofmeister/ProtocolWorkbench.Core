using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Frames
{

    /* Binary Protocol (11-byes of overhead + payload + auth) Little Endian
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

    public sealed record BinaryFrame(
        UInt16HbLb PayloadLength,
        UInt16HbLb Type,
        byte Flags,
        uint Seq,
        byte[] Payload,
        UInt16HbLb Crc16
    );
}
