namespace ProtocolWorkbench.Core.Enums
{
    public enum FieldKind
    {
        String,
        Integer,
        Number,
        Boolean,
        Object,     // JSON object
        Array,      // JSON array
        OneOf,      // union / polymorphic
        JsonEditor  // Raw JSON editor (object, array, unknown, complex)
    }
}
