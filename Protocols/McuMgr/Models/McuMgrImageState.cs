namespace ProtocolWorkbench.Core.Protocols.McuMgr.Models
{
    public sealed class McuMgrImageState
    {
        public IReadOnlyList<McuMgrImageSlot> Images { get; }

        public McuMgrImageState(IReadOnlyList<McuMgrImageSlot> images)
        {
            Images = images;
        }
    }
}
