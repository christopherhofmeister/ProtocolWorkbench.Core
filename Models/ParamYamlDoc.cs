namespace ProtocolWorkbench.Core.Models
{
    public sealed class ParamYamlDoc
    {
        public int Version { get; set; }
        public string Device { get; set; } = "";
        public List<ParamYamlItem> Params { get; set; } = new();
    }
}
