using ProtocolWorkbench.Core.Protocols.McuMgr.Models;
using ProtocolWorkbench.Core.Protocols.McuMgr.Transports;

namespace ProtocolWorkbench.Core.Protocols.McuMgr.Core;

public abstract class McuManager
{
    private byte _sequenceNumber;

    protected IMcuMgrTransport Transport { get; }

    protected abstract McuMgrGroup Group { get; }

    protected McuManager(IMcuMgrTransport transport)
    {
        Transport = transport ??
                    throw new ArgumentNullException(nameof(transport));
    }

    protected async Task<McuMgrResponse> SendAsync(
        McuMgrOp op,
        byte commandId,
        byte[]? payload = null,
        CancellationToken cancellationToken = default)
    {
        byte sequenceNumber = GetNextSequenceNumber();

        var request = new McuMgrRequest(
            op: op,
            group: Group,
            commandId: commandId,
            sequenceNumber: sequenceNumber,
            payload: payload);

        byte[] requestBytes = request.ToBytes();

        byte[] responseBytes =
            await Transport.SendAsync(
                requestBytes,
                cancellationToken);

        McuMgrResponse response =
            McuMgrResponse.Parse(responseBytes);

        ValidateResponse(sequenceNumber, response);

        return response;
    }

    private byte GetNextSequenceNumber()
    {
        unchecked
        {
            return _sequenceNumber++;
        }
    }

    private static void ValidateResponse(
        byte expectedSequenceNumber,
        McuMgrResponse response)
    {
        if (response.SequenceNumber != expectedSequenceNumber)
        {
            throw new InvalidOperationException(
                $"MCUmgr sequence mismatch. Expected={expectedSequenceNumber}, Actual={response.SequenceNumber}");
        }

        if (response.Op != McuMgrOp.ReadResponse &&
            response.Op != McuMgrOp.WriteResponse)
        {
            throw new InvalidOperationException(
                $"Unexpected MCUmgr response op: {response.Op}");
        }
    }
}