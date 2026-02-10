using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Services.AutoGenService
{
    public interface IAutoGenService
    {
        string Generate(AutoGenKind kind);
    }
}