using ProtocolWorkbench.Core.Protocols.McuMgr.Core;
using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using ProtocolWorkbench.Core.Protocols.McuMgr.Transports;
using System.Formats.Cbor;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Managers;

public sealed class ImageManager : McuManager
{
    protected override McuMgrGroup Group => McuMgrGroup.Image;

    public ImageManager(IMcuMgrTransport transport)
        : base(transport)
    {
    }

    /// <summary>
    /// image list
    /// </summary>
    public async Task<McuMgrResponse> ListAsync(CancellationToken cancellationToken = default)
    {
        byte[] payload = BuildListPayload();

        return await SendAsync(
            op: McuMgrOp.Read,
            commandId: 0,
            payload: payload,
            cancellationToken: cancellationToken);
    }

    public async Task<McuMgrResponse> TestAsync(string hashHex, CancellationToken cancellationToken = default)
    {
        byte[] payload = BuildStateWritePayload(hashHex, confirm: false);

        return await SendAsync(
            op: McuMgrOp.Write,
            commandId: 0,
            payload: payload,
            cancellationToken: cancellationToken);
    }

    public async Task<McuMgrResponse> ConfirmAsync(string hashHex, CancellationToken cancellationToken = default)
    {
        byte[] payload = BuildStateWritePayload(hashHex, confirm: true);

        return await SendAsync(
            op: McuMgrOp.Write,
            commandId: 0,
            payload: payload,
            cancellationToken: cancellationToken);
    }

    public async Task<McuMgrResponse> UploadAsync(
    int offset,
    byte[] data,
    int? totalLength = null,
    byte[]? sha = null,
    CancellationToken cancellationToken = default)
    {
        byte[] payload = BuildUploadPayload(
            offset,
            data,
            totalLength,
            sha);

        return await SendAsync(
            op: McuMgrOp.Write,
            commandId: 1,
            payload: payload,
            cancellationToken: cancellationToken);
    }

    public static byte[] BuildUploadPayloadForSizing(
    int offset,
    byte[] data,
    int? totalLength,
    byte[]? sha)
    {
        return BuildUploadPayload(
            offset,
            data,
            totalLength,
            sha);
    }

    private static byte[] BuildUploadPayload(
        int offset,
        byte[] data,
        int? totalLength,
        byte[]? sha)
    {
        int mapSize = 2;

        if (offset == 0 && totalLength.HasValue)
        {
            mapSize++;
        }

        if (offset == 0 && sha != null)
        {
            mapSize++;
        }

        var writer = new CborWriter();

        writer.WriteStartMap(mapSize);

        writer.WriteTextString("off");
        writer.WriteInt32(offset);

        writer.WriteTextString("data");
        writer.WriteByteString(data);

        if (offset == 0 && totalLength.HasValue)
        {
            writer.WriteTextString("len");
            writer.WriteInt32(totalLength.Value);
        }

        if (offset == 0 && sha != null)
        {
            writer.WriteTextString("sha");
            writer.WriteByteString(sha);
        }

        writer.WriteEndMap();

        return writer.Encode();
    }

    private static byte[] BuildStateWritePayload(string hashHex, bool confirm)
    {
        byte[] hash = Convert.FromHexString(hashHex);

        var writer = new CborWriter();

        writer.WriteStartMap(2);

        writer.WriteTextString("hash");
        writer.WriteByteString(hash);

        writer.WriteTextString("confirm");
        writer.WriteBoolean(confirm);

        writer.WriteEndMap();

        return writer.Encode();
    }

    private static byte[] BuildListPayload()
    {
        var writer = new CborWriter();

        writer.WriteStartMap(0);

        writer.WriteEndMap();

        return writer.Encode();
    }
}