namespace ProtocolWorkbench.Core.Protocols.McuMgr.Models
{
    public sealed class McuMgrImageSlot
    {
        public int Slot { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public bool Bootable { get; set; }
        public bool Pending { get; set; }
        public bool Confirmed { get; set; }
        public bool Active { get; set; }
        public bool Permanent { get; set; }

        public string SlotDisplay => $"Slot: {Slot}";
        public string VersionDisplay => $"Version: {Version}";
        public string ActiveDisplay => $"Active: {Active}";
        public string ConfirmedDisplay => $"Confirmed: {Confirmed}";
        public string PendingDisplay => $"Pending: {Pending}";
        public string BootableDisplay => $"Bootable: {Bootable}";
        public string HashDisplay => $"Hash: {Hash}";

        public override string ToString()
        {
            return $"Slot={Slot}, Version={Version}, " + $"Active={Active}, Confirmed={Confirmed}, Pending={Pending}";
        }
    }
}
