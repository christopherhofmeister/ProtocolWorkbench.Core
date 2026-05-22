using ProtocolWorkbench.Core.Protocols.McuMgr.Core;
using ProtocolWorkbench.Core.Protocols.McuMgr.Managers;
using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using ProtocolWorkbench.Core.Protocols.McuMgr.Parsers;
using ProtocolWorkbench.Core.Protocols.McuMgr.Transports;

namespace ProtocolWorkbench.Core.Services.McuMgr;

public sealed class McuMgrClientService : IMcuMgrClientService
{
    private readonly IMcuMgrTransport _transport;

    public McuMgrClientService(IMcuMgrTransport transport)
    {
        _transport = transport ??
                     throw new ArgumentNullException(nameof(transport));
    }

    public async Task<McuMgrImageState> GetImagesAsync(CancellationToken cancellationToken = default)
    {
        var imageManager = new ImageManager(_transport);

        var response =
            await imageManager.ListAsync(cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"MCUmgr image list failed. Rc={response.ReturnCode}");
        }

        return ImageStateParser.Parse(response.Payload);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var osManager = new OsManager(_transport);

        await osManager.ResetAsync(cancellationToken);
    }

    public async Task TestImageAsync(string hashHex, CancellationToken cancellationToken = default)
    {
        var imageManager = new ImageManager(_transport);

        var response = await imageManager.TestAsync(hashHex, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"MCUmgr image test failed. Rc={response.ReturnCode}");
        }
    }

    public async Task ConfirmImageAsync(string hashHex, CancellationToken cancellationToken = default)
    {
        var imageManager = new ImageManager(_transport);

        var response = await imageManager.ConfirmAsync(hashHex, cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"MCUmgr image confirm failed. Rc={response.ReturnCode}");
        }
    }

    public async Task UploadFirmwareAsync(
     byte[] firmware,
     IProgress<double>? progress = null,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(firmware);

        var imageManager = new ImageManager(_transport);

        int offset = 0;

        while (offset < firmware.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int chunkLength =
                CalculateUploadChunkLength(
                    imageManager,
                    firmware,
                    offset,
                    _transport.Mtu);

            byte[] chunk = new byte[chunkLength];

            Buffer.BlockCopy(
                firmware,
                offset,
                chunk,
                0,
                chunkLength);

            McuMgrResponse response =
                await imageManager.UploadAsync(
                    offset: offset,
                    data: chunk,
                    totalLength: offset == 0 ? firmware.Length : null,
                    sha: null,
                    cancellationToken: cancellationToken);

            if (!response.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Firmware upload failed. Rc={response.ReturnCode}");
            }

            McuMgrUploadResult result =
                ImageUploadParser.Parse(response.Payload);

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Firmware upload rejected. Rc={result.ReturnCode}");
            }

            if (result.Offset <= offset)
            {
                throw new InvalidOperationException(
                    $"Invalid upload offset returned by device. Device={result.Offset}, Local={offset}");
            }

            offset = result.Offset;

            progress?.Report((double)offset / firmware.Length);
        }
    }

    private static int CalculateUploadChunkLength(
        ImageManager imageManager,
        byte[] firmware,
        int offset,
        int mtu)
    {
        int remaining = firmware.Length - offset;

        int maxChunk = Math.Min(remaining, mtu);

        for (int chunkLength = maxChunk; chunkLength > 0; chunkLength--)
        {
            byte[] testChunk = new byte[chunkLength];

            Buffer.BlockCopy(
                firmware,
                offset,
                testChunk,
                0,
                chunkLength);

            byte[] payload =
                ImageManager.BuildUploadPayloadForSizing(
                    offset: offset,
                    data: testChunk,
                    totalLength: offset == 0 ? firmware.Length : null,
                    sha: null);

            int totalPacketLength =
                McuMgrHeader.HeaderLength + payload.Length;

            if (totalPacketLength <= mtu)
            {
                return chunkLength;
            }
        }

        throw new InvalidOperationException(
            $"Unable to fit MCUmgr upload packet within MTU={mtu}.");
    }
}