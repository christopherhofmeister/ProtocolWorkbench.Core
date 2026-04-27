using ProtocolWorkbench.Core.Enums;
using ProtocolWorkbench.Core.Protocols.Binary.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    public static class SetParameterPayloadDecoder
    {
        public static SetParameterResponse Decode(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 2)
                throw new InvalidOperationException($"SetParameter payload too short: {payload.Length}");

            var status = (RpcStatus)payload[0];
            var paramId = payload[1];

            return new SetParameterResponse(status, paramId);
        }
    }
}
