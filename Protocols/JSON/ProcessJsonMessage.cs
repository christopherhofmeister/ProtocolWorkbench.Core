using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkbench.Core.Services.UartDevice;

namespace ProtocolWorkBench.Core.Protocols
{
    public class ProcessJsonMessage
    {
        private UartDevice UartService;
        private ReceivedMessageService ReceivedMessageService;
        private CrcService CRCService;

        public ProcessJsonMessage(UartDevice uartService, ReceivedMessageService receivedMessageService, CrcService crcService)
        {
            UartService = uartService;
            ReceivedMessageService = receivedMessageService;
            CRCService = crcService;
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
            /*
            if (true == CRCService.MessageCRCCheck(result.Item2, result.Item1.U16Value))
            {
                string jsonMsg = ConversionService.ConvertByteListToASCII(result.Item2);
                MessageType msgType = ReceivedMessageService.DetermineMessageType(jsonMsg);
                ReceivedMessageService.AddMessageToQueue(msgType, jsonMsg);
            }
            */
        }
    }
}
