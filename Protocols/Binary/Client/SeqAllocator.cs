namespace ProtocolWorkbench.Core.Protocols.Binary.Client
{
    public sealed class SeqAllocator : ISeqAllocator
    {
        private uint _next = 1; // start at 1 (0 can be “no-seq” if you ever want)

        public uint Next() => unchecked(_next++);
    }
}
