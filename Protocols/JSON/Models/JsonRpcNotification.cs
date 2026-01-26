using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProtocolWorkBench.Core.Models.JsonRpc
{
    public class JsonRpcNotification
    {
        [JsonProperty(PropertyName = "jsonrpc", Order = 1)]
        public string JsonRPC { get; set; }

        [JsonProperty(PropertyName = "method", Order = 2)]
        public string Method { get; set; }

        [JsonProperty(PropertyName = "params", Order = 3, Required = Required.AllowNull)]
        public JArray Params = new JArray();
    }
}
