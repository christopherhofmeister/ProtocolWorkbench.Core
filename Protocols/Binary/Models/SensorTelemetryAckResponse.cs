using ProtocolWorkbench.Core.Enums;

namespace ProtocolWorkbench.Core.Protocols.Binary.Models
{
    /// <summary>
    /// SHP acknowledgment response to a Sensor: Telemetry request (groupid=1, id=0x000).
    ///
    /// The AP sends telemetry autonomously; the SHP acknowledges with a single status byte.
    /// A non-zero status indicates a protocol or state error on the SHP side only —
    /// the AP continues sending telemetry regardless.
    /// </summary>
    public class SensorTelemetryAckResponse
    {
        /// <summary>
        /// 0 = received and accepted. Non-zero = protocol/state error.
        /// </summary>
        public RpcStatus Status { get; set; }
    }
}
