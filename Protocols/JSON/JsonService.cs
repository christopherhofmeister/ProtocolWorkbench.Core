using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtocolWorkbench.Core.Services.CrcService;
using ProtocolWorkBench.Core.Models;
using ProtocolWorkBench.Core.Models.JsonRpc;

namespace ProtocolWorkBench.Core.Protocols.JSON
{
    public static class JsonService
    {
        public static string CreateJsonRPCRequestMessage(string methodName, int id, List<MessageParameter> msgParams)
        {
            JsonRpcRequest rpcReq = new JsonRpcRequest
            {
                Id = id,
                JsonRPC = "2.0",
                Method = methodName
            };

            foreach (MessageParameter p in msgParams)
            {
                JObject jObj = new JObject { { p.Name, p.Value } };
                rpcReq.Params.Add(jObj);
            }

            var msg1 = JsonConvert.SerializeObject(rpcReq);
            var cleanJson = JObject.Parse(msg1);
            return cleanJson.ToString();
        }

        public static string CreateJsonKeyValuePairObject(MessageParameter messageParameter)
        {
            JObject jObj1 = new JObject();
            jObj1.Add(ConversionService.ConvertMessageParmToJParam(messageParameter));
            var msg1 = JsonConvert.SerializeObject(jObj1);
            var cleanJson = JObject.Parse(msg1);
            return cleanJson.ToString();
        }

        public static string CreateJsonKeyValuePairObjects(List<MessageParameter> messageParameters)
        {
            string returnVal = null;

            if (messageParameters.Count > 0)
            {
                JObject jObj1 = new JObject();

                foreach (MessageParameter p in messageParameters)
                {
                    jObj1.Add(ConversionService.ConvertMessageParmToJParam(p));
                }

                return JsonConvert.SerializeObject(jObj1);
            }

            return returnVal;
        }

        public static string CreateJsonRPCNotificationMessage(string methodName, List<MessageParameter> msgParams)
        {
            JsonRpcNotification rpcNotification = new JsonRpcNotification
            {
                JsonRPC = "2.0",
                Method = methodName
            };

            foreach (MessageParameter p in msgParams)
            {
                JObject jObj = new JObject { { p.Name, p.Value } };
                rpcNotification.Params.Add(jObj);
            }

            var msg1 = JsonConvert.SerializeObject(rpcNotification);
            var cleanJson = JObject.Parse(msg1);
            return cleanJson.ToString();
        }

        public static string CreateJsonRPCResponseMessage(int id, List<MessageParameter> msgParams)
        {
            JsonRpcResponse rpcResponse = new JsonRpcResponse
            {
                Id = id,
                JsonRPC = "2.0"
            };

            foreach (MessageParameter p in msgParams)
            {
                JObject jObj = new JObject { { p.Name, p.Value } };
                rpcResponse.Result.Add(jObj);
            }

            var msg1 = JsonConvert.SerializeObject(rpcResponse);
            var cleanJson = JObject.Parse(msg1);
            return cleanJson.ToString();
        }

        public static List<byte> CreateJsonRPCRequestMessageCRC(
            ICrcService crc,
            string methodName,
            int id,
            List<MessageParameter> msgParams)
        {
            string msg = CreateJsonRPCRequestMessage(methodName, id, msgParams);
            byte[] msgBytes = ConversionService.ConvertStringToByteArray(msg);

            var result = msgBytes.ToList();

            UInt16HbLb crcValue = crc.ComputeCcitt16(msgBytes);
            result.Add(crcValue.Lb);
            result.Add(crcValue.Hb);

            return result;
        }

        public static List<byte> CreateJsonRPCNotificationMessageCRC(
            ICrcService crc,
            string methodName,
            List<MessageParameter> msgParams)
        {
            if (crc is null) throw new ArgumentNullException(nameof(crc));

            string msg = CreateJsonRPCNotificationMessage(methodName, msgParams);
            byte[] msgBytes = ConversionService.ConvertStringToByteArray(msg);

            // Build output (message bytes + crc16 LSB/HB)
            var msgLByte = msgBytes.ToList();

            UInt16HbLb crcValue = crc.ComputeCcitt16(msgBytes);
            msgLByte.Add(crcValue.Lb);
            msgLByte.Add(crcValue.Hb);

            return msgLByte;
        }

        public static List<byte> CreateJsonRPCResponseMessageCRC(
             ICrcService crc,
             int id,
             List<MessageParameter> msgParams)
        {
            if (crc is null) throw new ArgumentNullException(nameof(crc));

            string msg = CreateJsonRPCResponseMessage(id, msgParams);
            byte[] msgBytes = ConversionService.ConvertStringToByteArray(msg);

            var msgLByte = msgBytes.ToList();

            UInt16HbLb crcValue = crc.ComputeCcitt16(msgBytes);
            msgLByte.Add(crcValue.Lb);
            msgLByte.Add(crcValue.Hb);

            return msgLByte;
        }

        public static List<MessageParameter> GetJArrayValues(JArray jArray)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();

            foreach (JObject jObj in jArray)
            {
                foreach (JProperty p in jObj.Properties())
                {
                    MessageParameter thisMsgParam = new MessageParameter();
                    thisMsgParam.Name = p.Name;
                    thisMsgParam.Value = ((string)p.Value);
                    msgParams.Add(thisMsgParam);
                }
            }

            return msgParams;
        }

        public static List<MessageParameter> GetJObjValues(JObject jObjs)
        {
            List<MessageParameter> msgParams = new List<MessageParameter>();

            foreach (JProperty p in jObjs.Properties())
            {
                MessageParameter thisMsgParam = new MessageParameter();
                thisMsgParam.Name = p.Name;
                thisMsgParam.Value = ((string)p.Value);
                msgParams.Add(thisMsgParam);
            }

            return msgParams;
        }

    }
}
