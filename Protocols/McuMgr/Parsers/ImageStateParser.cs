using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using System.Formats.Cbor;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Parsers;

public static class ImageStateParser
{
    public static McuMgrImageState Parse(byte[] payload)
    {
        var reader = new CborReader(payload);

        int? mapLength = reader.ReadStartMap();

        List<McuMgrImageSlot> images = [];

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            string key = reader.ReadTextString();

            if (key == "images")
            {
                images = ParseImages(reader);
            }
            else
            {
                reader.SkipValue();
            }
        }

        reader.ReadEndMap();

        return new McuMgrImageState(images);
    }

    private static List<McuMgrImageSlot> ParseImages(
        CborReader reader)
    {
        List<McuMgrImageSlot> images = [];

        int? arrayLength = reader.ReadStartArray();

        while (reader.PeekState() != CborReaderState.EndArray)
        {
            images.Add(ParseImage(reader));
        }

        reader.ReadEndArray();

        return images;
    }

    private static McuMgrImageSlot ParseImage(
        CborReader reader)
    {
        var image = new McuMgrImageSlot();

        int? mapLength = reader.ReadStartMap();

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            string key = reader.ReadTextString();

            switch (key)
            {
                case "slot":
                    image.Slot = reader.ReadInt32();
                    break;

                case "version":
                    image.Version = reader.ReadTextString();
                    break;

                case "hash":
                    image.Hash =
                        Convert.ToHexString(
                            reader.ReadByteString());
                    break;

                case "bootable":
                    image.Bootable = reader.ReadBoolean();
                    break;

                case "pending":
                    image.Pending = reader.ReadBoolean();
                    break;

                case "confirmed":
                    image.Confirmed = reader.ReadBoolean();
                    break;

                case "active":
                    image.Active = reader.ReadBoolean();
                    break;

                case "permanent":
                    image.Permanent = reader.ReadBoolean();
                    break;

                default:
                    reader.SkipValue();
                    break;
            }
        }

        reader.ReadEndMap();

        return image;
    }
}