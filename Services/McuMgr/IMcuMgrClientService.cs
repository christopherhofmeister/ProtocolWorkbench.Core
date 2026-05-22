using ProtocolWorkbench.Core.Protocols.McuMgr.Models;

namespace ProtocolWorkbench.Core.Services.McuMgr
{
    public interface IMcuMgrClientService
    {
        Task<McuMgrImageState> GetImagesAsync(CancellationToken cancellationToken = default);

        Task ResetAsync(CancellationToken cancellationToken = default);

        Task TestImageAsync(string hashHex, CancellationToken cancellationToken = default);

        Task ConfirmImageAsync(string hashHex, CancellationToken cancellationToken = default);

        Task UploadFirmwareAsync(byte[] firmware, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    }
}
