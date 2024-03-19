using ClientNetworking;

namespace Omukade.Cheyenne.ClientConnections
{
    public class DebugHandlerClientConnection : IClientConnection
    {
        public DebugHandlerClientConnection(Action<DebugHandlerClientConnection, object> handler)
        {
            this.MessageReceived = handler;
        }

        public event Action<DebugHandlerClientConnection, object> MessageReceived;

        public PlayerMetadata? Tag { get; set; }

        public bool IsOpen => true;

        public void DisconnectClientImmediately()
        {

        }

        public void SendMessageEnquued_EXPERIMENTAL(object payload, SerializationFormat format = SerializationFormat.JSON)
        {
            MessageReceived.Invoke(this, payload);
        }
    }
}
