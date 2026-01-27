using PeterO.Cbor;
using ProtocolWorkbench.Core.Services.UartDevice;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Protocols.SMP;
using System;
using static ProtocolWorkBench.Core.Models.MessageTypes;

namespace ProtocolWorkBench.Core.Protocols
{
    public class ProcessSmpMessage
    {
        private UartDevice UartService;
        private ReceivedMessageService ReceivedMessageService;

        public ProcessSmpMessage(UartDevice uartService, ReceivedMessageService receivedMessageService)
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
            var result = UartService.RemoveCRCFromMessage(rxMessage.Item1.FullMessage);
            if (true == CRCService.MessageCRCCheck(result.Item2, result.Item1.U16Value))
            {
                SmpHeader smpHeader = SMPService.GetSmpHeaderFromDataLittleEndian(result.Item2);
                /* remove the header */
                result.Item2.RemoveRange(0, SMPService.SMP_HEADER_LENGTH);

                var obj1a = CBORObject.DecodeFromBytes(result.Item2.ToArray());
                string jsonMsg = obj1a.ToJSONString();
                MessageType msgType = ReceivedMessageService.DetermineMessageType(jsonMsg);
                ReceivedMessageService.AddMessageToQueue(msgType, jsonMsg);
            }
        }
    }
}
