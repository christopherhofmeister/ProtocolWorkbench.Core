namespace ProtocolWorkbench.Core.Models
{
    public sealed class ParamYamlItem
    {
        public byte Id { get; set; }
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Access { get; set; } = "";  // readonly/writeonly/readwrite
        public string CType { get; set; } = "";   // STRING, UINT32, etc
        public int Max_Len { get; set; }          // max_len in YAML -> Max_Len in C# unless you map naming
        public string? Summary { get; set; }
    }
}
