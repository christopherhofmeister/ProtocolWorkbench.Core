using ProtocolWorkbench.Core.Models;

namespace ProtocolWorkbench.Core.Services.CodeGen
{
    public interface IAppParamsFirmwareGenerator
    {
        (string Header, string Source) Generate(ParamYamlDoc doc);
    }
}