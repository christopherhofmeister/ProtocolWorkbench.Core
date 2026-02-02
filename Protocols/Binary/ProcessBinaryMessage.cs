using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkbench.Core.Services.UartDevice;
using ProtocolWorkBench.Core.Protocols.Binary.Models;

namespace ProtocolWorkBench.Core.Protocols.Binary
{
    public class ProcessBinaryMessage
    {
        public const byte SOF = 1;
        public const byte EOF = 4;
        public const byte MinMessageLength = 13;

        private UartDevice _uartService;

        private ReceivedMessageService _receivedMessageService;

        public ProcessBinaryMessage(UartDevice uartService, ReceivedMessageService receivedMessageService)
        {
            _uartService = uartService;
            _receivedMessageService = receivedMessageService;
        }

        public void EnableProcessing()
        {
            _uartService.RxMsgQueuedEvent += UartService_RxMsgQueuedEvent;
        }

        public void DisableProcessing()
        {
            _uartService.RxMsgQueuedEvent -= UartService_RxMsgQueuedEvent;
        }

        private void UartService_RxMsgQueuedEvent(object sender, EventArgs e)
        {
            ProcessReceivedMessage();
        }

        private void ProcessReceivedMessage()
        {
            var rxMessage = _uartService.DequeueRxMessage();
            var msg = rxMessage.Item1.FullMessage;

            if (msg == null || msg.Count < 13) return;
            if (msg[0] != SOF || msg[^1] != EOF) return;

            var binaryProtocol = new BinaryProtocol
            {
                StartOfFrame = msg[0],
                EndOfFrame = msg[^1],
            };

            binaryProtocol.Length.Lb = msg[1];
            binaryProtocol.Length.Hb = msg[2];
            binaryProtocol.MesssageType.Lb = msg[3];
            binaryProtocol.MesssageType.Hb = msg[4];

            int payloadStart = 10;
            int crcIndex = msg.Count - 3;

            if (crcIndex < payloadStart) return;

            for (int i = payloadStart; i < crcIndex; i++)
                binaryProtocol.Payload.Add(msg[i]);

            binaryProtocol.CRC.Lb = msg[crcIndex];
            binaryProtocol.CRC.Hb = msg[crcIndex + 1];

            var msgNoCRC = msg.Take(msg.Count - 3).ToList(); // <-- copy
            if (!CRCService.MessageCRCCheck(msgNoCRC, binaryProtocol.CRC.U16Value)) return;

            _receivedMessageService.AddMessageToQueue(
                ProtocolWorkBench.Core.Models.MessageTypes.MessageType.BinaryB,
                binaryProtocol
            );
        }
    }
}
