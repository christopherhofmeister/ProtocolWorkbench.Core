
namespace ProtocolWorkBench.Core.Models
{
    public static class ProtocolDefinitions
    {
        public enum ProtocolTypes : int
        {
            JSON,
            SMP,
            SMPCONSOLE,
            BINARYB
        }

        public const string JSON_RPC = "JSON-RPC";

        public const string SMP = "SMP";

        public const string SMPCONSOLE = "SMPCONSOLE";

        public const string BINARYB = "BINARYB";
    }
}
