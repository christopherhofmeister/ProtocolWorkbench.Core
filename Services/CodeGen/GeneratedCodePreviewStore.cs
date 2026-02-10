namespace ProtocolWorkbench.Core.Services.CodeGen
{
    public sealed class GeneratedCodePreviewStore : IGeneratedCodePreviewStore
    {
        public string Header { get; set; } = "";
        public string Source { get; set; } = "";
        public string Title { get; set; } = "Generated Files";
    }
}
