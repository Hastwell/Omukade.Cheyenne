using Platform.Sdk;

namespace Omukade.Cheyenne.ClientConnections
{
    public interface IClientConnection
    {
        public void SendMessageEnquued_EXPERIMENTAL(object payload, SerializationFormat format = SerializationFormat.JSON);
        public void DisconnectClientImmediately();
        public PlayerMetadata? Tag { get; set; }
        public bool IsOpen { get; }
    }
}
