using ProtocolWorkBench.Core.Models;

namespace ProtocolWorkbench.Core.Services.TypeHelper
{
    public static class TypeSuggestion
    {
        public static string SuggestCType(string? jsonType)
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
    }
}
