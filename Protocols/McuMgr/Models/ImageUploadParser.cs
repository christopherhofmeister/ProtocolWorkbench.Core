using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using System.Formats.Cbor;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Parsers;

public static class ImageUploadParser
{
    public static McuMgrUploadResult Parse(byte[] payload)
    {
        var result = new McuMgrUploadResult();

        var reader = new CborReader(payload);

        int? mapLength = reader.ReadStartMap();

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            string key = reader.ReadTextString();

            switch (key)
            {
                case "rc":
                    result.ReturnCode = reader.ReadInt32();
                    break;

                case "off":
                    result.Offset = reader.ReadInt32();
                    break;

                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        return result;
    }
}