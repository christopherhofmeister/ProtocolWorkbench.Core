using ProtocolWorkbench.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ProtocolWorkbench.Core.Services.Yaml;

public sealed class ParamYamlService : IParamYamlService
{
    private static readonly IDeserializer _deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private static readonly ISerializer _serializer =
        new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .Build();

    public async Task<(ParamYamlDoc Doc, string Raw)> LoadAsync(string path)
    {
        var raw = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        var doc = Deserialize(raw);
        return (doc, raw);
    }

    public async Task SaveAsync(string path, ParamYamlDoc doc)
    {
        var yaml = Serialize(doc);
        await File.WriteAllTextAsync(path, yaml).ConfigureAwait(false);
    }

    public ParamYamlDoc Deserialize(string rawYaml)
    {
        var doc = _deserializer.Deserialize<ParamYamlDoc>(rawYaml) ?? new ParamYamlDoc();
        doc.Params ??= new List<ParamYamlItem>();
        return doc;
    }

    public string Serialize(ParamYamlDoc doc)
    {
        doc.Params ??= new List<ParamYamlItem>();
        doc.Params = doc.Params.OrderBy(p => p.Id).ToList();
        return _serializer.Serialize(doc);
    }
}