using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProtocolWorkBench.Core.Models.JsonRpc
{
    public class JsonRpcResponse
    {
        [JsonProperty(PropertyName = "jsonrpc", Order = 1)]
        public string JsonRPC { get; set; }

        [JsonProperty(PropertyName = "id", Order = 3)]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "result", Order = 3, Required = Required.AllowNull)]
        public JArray Result = new JArray();
    }
}
