using ProtocolWorkbench.Core.Enums;
using ProtocolWorkbench.Core.Protocols.Binary.Models;

namespace ProtocolWorkbench.Core.Protocols.Binary.Helpers
{
    /// <summary>
    /// Decodes SHP response payloads for groupid=1 (Sensor Data) messages.
    /// </summary>
    public static class SensorPayloadDecoder
    {
        /// <summary>
        /// Decodes the SHP acknowledgment response to a Sensor: Telemetry request.
        ///
        /// Wire layout (1 byte):
        ///   [0] status : uint8  — 0 = received and accepted; non-zero = protocol/state error.
        ///
        /// Note: a non-zero status does not mean the AP should stop sending telemetry.
        /// The AP sends autonomously regardless of the SHP response.
        /// </summary>
        public static SensorTelemetryAckResponse DecodeTelemetryAck(byte[] payload)
        {
            if (payload is null || payload.Length < 1)
                return new SensorTelemetryAckResponse { Status = RpcStatus.InvalidArg };

            return new SensorTelemetryAckResponse
            {
                Status = (RpcStatus)payload[0]
            };
        }
    }
}
