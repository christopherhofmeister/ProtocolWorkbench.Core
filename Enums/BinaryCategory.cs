namespace ProtocolWorkbench.Core.Enums
{
    public enum BinaryCategory : byte
    {
        Application = 0x0,
        WifiRadioTest = 0x1,
        BluetoothRadioTest = 0x2,
        Radio802154Test = 0x3,
        FirmwareBootloader = 0x4,
        ManufacturingTec = 0x5,

        Reserved6 = 0x6,
        Reserved7 = 0x7,
        Reserved8 = 0x8,
        Reserved9 = 0x9,
        ReservedA = 0xA,
        ReservedB = 0xB,
        ReservedC = 0xC,
        ReservedD = 0xD,
        ReservedE = 0xE,

        VendorExperimental = 0xF
    }
}
