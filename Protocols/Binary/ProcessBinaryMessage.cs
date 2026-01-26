using ProtocolWorkBench.Core.Protocols.Binary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtocolWorkBench.Core.Protocols.Binary
{
    public class ProcessBinaryMessage
    {
        public const byte SOF = 1;
        public const byte EOF = 4;

        private UartDevice UartService;

        private ReceivedMessageService ReceivedMessageService;

        public ProcessBinaryMessage(UartDevice uartService, ReceivedMessageService receivedMessageService)
        {
            UartService = uartService;
            ReceivedMessageService = receivedMessageService;
        }

        public void EnableProcessing()
        {
            UartService.RxMsgQueuedEvent += UartService_RxMsgQueuedEvent;
        }

        private void UartService_RxMsgQueuedEvent(object sender, EventArgs e)
        {
            ProcessReceivedMessage();
        }

        private void ProcessReceivedMessage()
        {
            var rxMessage = UartService.DequeueRxMessage();

            BinaryProtocolB binaryProtocolB = new BinaryProtocolB();

            binaryProtocolB.StartOfFrame = rxMessage.Item1.FullMessage[0];
            binaryProtocolB.Length.Lb = rxMessage.Item1.FullMessage[1];
            binaryProtocolB.Length.Hb = rxMessage.Item1.FullMessage[2];
            binaryProtocolB.MesssageType = rxMessage.Item1.FullMessage[3];
            
            /* SOF = 1
             * LengthLsb = 10
             * LengthMsb = 0
             * MessageType = 11
             * Payload = 7 7 7 
             * CRCLSB = 2
             * CRCMSB = 3
             * EOF = 4
            /* 1, 10, 0, 11, 7, 7, 7, 2, 3, 4 */

            int length = rxMessage.Item1.FullMessage.Count;
            for (int i = 4; i < length - 3; i++ )
            {
                binaryProtocolB.Payload.Add(rxMessage.Item1.FullMessage[i]);
            }
            
            binaryProtocolB.CRC.Lb = rxMessage.Item1.FullMessage[length - 3];
            binaryProtocolB.CRC.Hb = rxMessage.Item1.FullMessage[length - 2];
            binaryProtocolB.EndOfFrame = rxMessage.Item1.FullMessage[length - 1];

            List<byte> msgNoCRC = rxMessage.Item1.FullMessage;
            msgNoCRC.RemoveRange(msgNoCRC.Count - 3, 3);

            if ((SOF == binaryProtocolB.StartOfFrame) && (EOF == binaryProtocolB.EndOfFrame))
            {
                if (true == CRCService.MessageCRCCheck(msgNoCRC, binaryProtocolB.CRC.U16Value))
                {
                    ReceivedMessageService.AddMessageToQueue(ProtocolWorkBench.Core.Models.MessageTypes.MessageType.BinaryB, binaryProtocolB);
                }
            }
        }
    }
}
