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
    }
}
