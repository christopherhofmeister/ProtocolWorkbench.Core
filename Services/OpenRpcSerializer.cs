using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProtocolWorkbench.Core.Services
{
    public static class OpenRpcSerializer
    {
        public static string SerializeOrdered(object api)
        {
            // 1) Serialize like you already do
            var json = JsonConvert.SerializeObject(
                api,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            // 2) Reorder "properties" objects by each property's x-fieldorder
            var root = JToken.Parse(json);
            ReorderAllPropertiesObjects(root);

            // 3) Emit pretty JSON again
            return root.ToString(Formatting.Indented);
        }

        private static void ReorderAllPropertiesObjects(JToken token)
        {
            if (token is JObject obj)
            {
                // If this object contains a JSON Schema "properties" object, reorder it
                if (obj.TryGetValue("properties", out var propsTok) && propsTok is JObject propsObj)
                {
                    var ordered = new JObject(
                        propsObj.Properties()
                            .OrderBy(p => GetFieldOrder(p.Value))
                            .ThenBy(p => p.Name, StringComparer.Ordinal)
                            .Select(p => new JProperty(p.Name, p.Value))
                    );

                    obj["properties"] = ordered;
                }

                // Recurse into children
                foreach (var child in obj.Properties().Select(p => p.Value))
                {
                    ReorderAllPropertiesObjects(child);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr)
                {
                    ReorderAllPropertiesObjects(item);
                }
            }
        }

        private static int GetFieldOrder(JToken schemaNode)
        {
            // Your extension name: "x-fieldorder"
            var t = schemaNode?["x-fieldorder"];
            if (t == null) return int.MaxValue;

            if (t.Type == JTokenType.Integer) return t.Value<int>();
            if (t.Type == JTokenType.Float) return (int)t.Value<double>();
            if (t.Type == JTokenType.String && int.TryParse(t.Value<string>(), out var v)) return v;

            return int.MaxValue;
        }
    }
}
