using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Services.TypeHelper
{
    public static class TypeSuggestion
    {
        public static CTypes SuggestCType(string? jsonType)
            => (jsonType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "string" => CTypes.STRING,
                "boolean" => CTypes.BOOL,
                "integer" => CTypes.UINT32,
                "number" => CTypes.FLOAT,
                "array" => CTypes.BYTE_ARRAY,
                "object" => CTypes.STRING,   // pragmatic fallback
                _ => CTypes.STRING
            };

        public static string SuggestCTypeString(string? jsonType)
            => (jsonType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "string" => "string",
                "boolean" => "bool",
                "integer" => "uint32_t",
                "number" => "float",
                "array" => "uint8_t[]",
                "object" => "string",   // pragmatic fallback
                _ => "string"
            };
    }
}
