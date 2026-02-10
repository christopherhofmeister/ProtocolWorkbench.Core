
using ProtocolWorkbench.Core.Models;

namespace ProtocolWorkbench.Core.Services.Yaml
{
    public interface IParamYamlService
    {
        Task<(ParamYamlDoc Doc, string Raw)> LoadAsync(string path);
        Task SaveAsync(string path, ParamYamlDoc doc);

        // For in-memory parse/build from the Editor:
        ParamYamlDoc Deserialize(string rawYaml);
        string Serialize(ParamYamlDoc doc);
    }
}