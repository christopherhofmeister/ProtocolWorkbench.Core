using Newtonsoft.Json;
using ProtocolWorkBench.Core.Models.JsonRpc;
using ProtocolWorkBench.Core.Protocols.Binary.Models;
using ProtocolWorkBench.Core.Protocols.SMP.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using static ProtocolWorkBench.Core.Models.MessageTypes;

namespace ProtocolWorkBench.Core
{
    public class ReceivedMessageService
    {
        private ConcurrentQueue<JsonRpcResponse> cqResponseMessages = new ConcurrentQueue<JsonRpcResponse>();

        private ConcurrentQueue<JsonRpcNotification> cqNotificationMessages = new ConcurrentQueue<JsonRpcNotification>();

        private ConcurrentQueue<JsonRpcRequest> cqRequestMessages = new ConcurrentQueue<JsonRpcRequest>();

        private ConcurrentQueue<BinaryProtocol> cqBinaryBMessages = new ConcurrentQueue<BinaryProtocol>();

        private ConcurrentQueue<SmpMessage> cqSmpMessages = new ConcurrentQueue<SmpMessage>();

        public event EventHandler ResponseMsgEvent;
        public event EventHandler NotificationMsgEvent;
        public event EventHandler RequestMsgEvent;
        public event EventHandler BinaryBMsgEvent;
        public event EventHandler<string> SmpEvent;

        private string ComPort;
        public void SetReceivedMessageServiceComPort(string comport)
        {
            ComPort = comport;
        }

        public void OnResponseMsgEvent()
        {
            EventHandler eh = ResponseMsgEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        public void OnNotificationMsgEvent()
        {
            EventHandler eh = NotificationMsgEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        public void OnRequestMsgEvent()
        {
            EventHandler eh = RequestMsgEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        public void OnBinaryBMsgEvent()
        {
            EventHandler eh = BinaryBMsgEvent;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }

        public void OnSmpEvent(string comPort)
        {
            EventHandler<string> eh = SmpEvent;
            if (eh != null)
            {
                eh(this, comPort);
            }
        }

        public MessageType DetermineMessageType(string jsonMsg)
        {
            MessageType msgType = MessageType.Unknown;

            if (jsonMsg.ToLower().Contains("id") == false)
            {
                msgType = MessageType.Notification;
            }

            if ((jsonMsg.ToLower().Contains("id")) && ((jsonMsg.ToLower().Contains("result")) || (jsonMsg.ToLower().Contains("r"))))
            {
                msgType = MessageType.Response;
            }

            if ((jsonMsg.ToLower().Contains("id")) && (jsonMsg.ToLower().Contains("method")))
            {
                msgType = MessageType.Request;
            }

            return msgType;
        }

        public void AddMessageToQueue(MessageType messageType, BinaryProtocol binaryMessage)
        {
            switch (messageType)
            {
                case MessageType.BinaryB:
                    AddBinary(binaryMessage);
                    break;
            }
        }

        public void AddMessageToQueue(SmpMessage frame)
        {
            AddSmpConsoleMsg(frame);
        }

        public void AddMessageToQueue(MessageType messageType, string jsonMsg)
        {
            switch (messageType)
            {
                case MessageType.Request:
                    JsonRpcRequest rpcMsgRequest = JsonConvert.DeserializeObject<JsonRpcRequest>(jsonMsg);
                    AddRequest(rpcMsgRequest);
                    break;

                case MessageType.Response:
                    JsonRpcResponse rpcMsgResponse = JsonConvert.DeserializeObject<JsonRpcResponse>(jsonMsg);
                    AddResponse(rpcMsgResponse);
                    break;

                case MessageType.Notification:
                    JsonRpcNotification rpcMsgNotification = JsonConvert.DeserializeObject<JsonRpcNotification>(jsonMsg);
                    AddNotification(rpcMsgNotification);
                    break;
            }
        }

        public (bool, JsonRpcResponse) GetResponse()
        {
            bool foundMessage = false;
            JsonRpcResponse rpcMsg = new JsonRpcResponse();
            if (cqResponseMessages.Count > 0)
            {
                cqResponseMessages.TryDequeue(out rpcMsg);
                foundMessage = true;
            }
            return (foundMessage, rpcMsg);
        }

        public (bool, JsonRpcResponse) GetResponse(int id)
        {
            bool foundMessage = false;
            JsonRpcResponse rpcMsg = new JsonRpcResponse();
            List<JsonRpcResponse> responses = new List<JsonRpcResponse>();

            while ((cqResponseMessages.Count > 0) && (foundMessage == false))
            {
                JsonRpcResponse cqValue = new JsonRpcResponse();
                cqResponseMessages.TryDequeue(out cqValue);
                if (cqValue.Id == id)
                {
                    /* matching message */
                    rpcMsg = cqValue;
                    foundMessage = true;
                }
                else
                {
                    /* add to list to add back later */
                    responses.Add(cqValue);
                }
            }

            /* add non matching messages back into the queue */
            foreach (JsonRpcResponse nonMatchingMessage in responses)
            {
                cqResponseMessages.Enqueue(nonMatchingMessage);
            }

            return (foundMessage, rpcMsg);
        }

        public int GetResponseQueueCount()
        {
            return cqResponseMessages.Count;
        }

        public (bool, JsonRpcNotification) GetNotification(string method)
        {
            bool foundMessage = false;
            JsonRpcNotification rpcMsg = new JsonRpcNotification();
            List<JsonRpcNotification> notifications = new List<JsonRpcNotification>();

            while ((cqNotificationMessages.Count > 0) && (foundMessage == false))
            {
                JsonRpcNotification cqValue = new JsonRpcNotification();
                cqNotificationMessages.TryDequeue(out cqValue);
                if (cqValue.Method == method)
                {
                    /* matching message */
                    rpcMsg = cqValue;
                    foundMessage = true;
                }
                else
                {
                    /* add to list to add back later */
                    notifications.Add(cqValue);
                }
            }

            /* add non matching messages back into the queue */
            foreach (JsonRpcNotification nonMatchingMessage in notifications)
            {
                cqNotificationMessages.Enqueue(nonMatchingMessage);
            }

            return (foundMessage, rpcMsg);
        }

        public (bool, JsonRpcNotification) GetNotification()
        {
            bool foundMessage = false;
            JsonRpcNotification rpcMsg = new JsonRpcNotification();

            if (cqNotificationMessages.Count > 0)
            {
                cqNotificationMessages.TryDequeue(out rpcMsg);
                foundMessage = true;
            }
            return (foundMessage, rpcMsg);
        }

        public int GetNotificationQueueCount()
        {
            return cqNotificationMessages.Count;
        }

        public (bool, JsonRpcRequest) GetRequest()
        {
            bool foundMessage = false;
            JsonRpcRequest rpcMsg = new JsonRpcRequest();
            if (cqRequestMessages.Count > 0)
            {
                cqRequestMessages.TryDequeue(out rpcMsg);
                foundMessage = true;
            }
            return (foundMessage, rpcMsg);
        }

        public int GetRequestQueueCount()
        {
            return cqRequestMessages.Count;
        }

        private void AddResponse(JsonRpcResponse rpcMsg)
        {
            cqResponseMessages.Enqueue(rpcMsg);
            OnResponseMsgEvent();
        }

        private void AddSmpConsoleMsg(SmpMessage smpMsg)
        {
            cqSmpMessages.Enqueue(smpMsg);
            OnSmpEvent(ComPort);
        }

        public (bool Success, SmpMessage SmpMessage) GetSmpMsg()
        {
            bool foundMessage = false;
            SmpMessage frame = new SmpMessage();
            Debug.WriteLine($"Q Count GetSmpMsg on {ComPort} = {cqSmpMessages.Count}");
            if (cqSmpMessages.Count > 0)
            {
                cqSmpMessages.TryDequeue(out frame);
                foundMessage = true;
            }
            return (foundMessage, frame);
        }

        public int GetSmpQueueCount()
        {
            return cqSmpMessages.Count;
        }

        private void AddNotification(JsonRpcNotification rpcMsg)
        {
            cqNotificationMessages.Enqueue(rpcMsg);
            OnNotificationMsgEvent();
        }

        private void AddRequest(JsonRpcRequest rpcMsg)
        {
            cqRequestMessages.Enqueue(rpcMsg);
            OnRequestMsgEvent();
        }

        private void AddBinary(BinaryProtocol binBMsg)
        {
            cqBinaryBMessages.Enqueue(binBMsg);
            OnBinaryBMsgEvent();
        }

        public (bool, BinaryProtocol) GetBinaryFromQueue()
        {
            bool foundMessage = false;
            BinaryProtocol msg = new BinaryProtocol();

            if (cqBinaryBMessages.Count > 0)
            {
                cqBinaryBMessages.TryDequeue(out msg);
                foundMessage = true;
            }

            return (foundMessage, msg);
        }

    }
}
