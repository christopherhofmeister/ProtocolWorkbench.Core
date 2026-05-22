using ProtocolWorkbench.Core.Protocols.McuMgr.Core;
using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using ProtocolWorkbench.Core.Protocols.McuMgr.Transports;
using System.Formats.Cbor;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Managers;

public sealed class OsManager : McuManager
{
    protected override McuMgrGroup Group => McuMgrGroup.Os;

    public OsManager(IMcuMgrTransport transport)
        : base(transport)
    {
    }

    public async Task ResetAsync(
        CancellationToken cancellationToken = default)
    {
        byte[] payload = BuildResetPayload();

        McuMgrResponse response =
            await SendAsync(
                op: McuMgrOp.Write,
                commandId: 5,
                payload: payload,
                cancellationToken: cancellationToken);

        if (!response.IsSuccess)
        {
            throw new InvalidOperationException(
                $"MCUmgr reset failed. Rc={response.ReturnCode}");
        }
    }

    private static byte[] BuildResetPayload()
    {
        var writer = new CborWriter();

        writer.WriteStartMap(0);

        writer.WriteEndMap();

        return writer.Encode();
    }
}