namespace ProtocolWorkbench.Core.Services.CodeGen
{
    public interface IGeneratedCodePreviewStore
    {
        string Header { get; set; }
        string Source { get; set; }
        string Title { get; set; }
    }
}