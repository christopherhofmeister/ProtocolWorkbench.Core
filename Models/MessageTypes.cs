namespace ProtocolWorkBench.Core.Models
{
    public class MessageTypes
    {
        public enum MessageType
        {
            Unknown = 0,
            Request = 1,
            Response = 2,
            Notification = 3,
            BinaryB = 4
        };
    }
}
