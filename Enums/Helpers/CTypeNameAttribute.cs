namespace ProtocolWorkbench.Core.Enums.Helpers
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CTypeNameAttribute : Attribute
    {
        public string Name { get; }
        public CTypeNameAttribute(string name) => Name = name;
    }
}
