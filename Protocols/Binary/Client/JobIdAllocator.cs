namespace ProtocolWorkbench.Core.Protocols.Binary.Client
{
    public sealed class JobIdAllocator : IJobIdAllocator
    {
        private ushort _next = 1; // 0 reserved if you want

        public ushort Next(HashSet<ushort> inFlight)
        {
            // find next id not currently in use
            for (int i = 0; i < ushort.MaxValue; i++)
            {
                var id = _next++;
                if (_next == 0) _next = 1;

                if (!inFlight.Contains(id))
                    return id;
            }

            throw new InvalidOperationException("No free JobIds (too many in-flight jobs).");
        }
    }
}
