using ProtocolWorkbench.Core.Enums;
using YamlDotNet.Serialization;

namespace ProtocolWorkbench.Core.Models
{
    public class ParamYamlItem
    {
        [YamlMember(Alias = "id")]
        public byte Id { get; set; }

        [YamlMember(Alias = "key")]
        public string? Key { get; set; }

        [YamlMember(Alias = "name")]
        public string? Name { get; set; }

        [YamlMember(Alias = "access")]
        public string? Access { get; set; }

        [YamlMember(Alias = "ctype")]
        public CTypes CTypeEnum { get; set; }

        [YamlMember(Alias = "max_len")]
        public int Max_Len { get; set; }

        [YamlMember(Alias = "summary")]
        public string? Summary { get; set; }

        [YamlMember(Alias = "default")]
        public string? Default { get; set; }

        [YamlMember(Alias = "debug_access")]
        public string? DebugAccess { get; set; }

        [YamlMember(Alias = "factory_access")]
        public string? FactoryAccess { get; set; }

        [YamlMember(Alias = "storage")]
        public string? Storage { get; set; }

        [YamlMember(Alias = "storage_id")]
        public ushort? StorageId { get; set; }

        [YamlIgnore]
        public AccessTypes AccessEnum => ParseAccess(Access);

        [YamlIgnore]
        public AccessTypes DebugAccessEnum => ParseAccess(
            string.IsNullOrWhiteSpace(DebugAccess) ? Access : DebugAccess);

        [YamlIgnore]
        public AccessTypes FactoryAccessEnum => ParseAccess(
            string.IsNullOrWhiteSpace(FactoryAccess) ? Access : FactoryAccess);

        private static AccessTypes ParseAccess(string? s)
        {
            return (s ?? "").Trim().ToLowerInvariant() switch
            {
                "readonly" => AccessTypes.ReadOnly,
                "writeonly" => AccessTypes.WriteOnly,
                "readwrite" => AccessTypes.ReadWrite,
                _ => AccessTypes.None
            };
        }
    }
}