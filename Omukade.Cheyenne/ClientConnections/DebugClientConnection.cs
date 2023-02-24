using Platform.Sdk;
using System.Collections.Concurrent;

namespace Omukade.Cheyenne.ClientConnections
{
    public class DebugClientConnection : IClientConnection
    {
        public Queue<object> MessagesToClient = new Queue<object>(10);

        public PlayerMetadata? Tag { get; set; }

        public bool IsOpen => true;

        public void DisconnectClientImmediately()
        {

        }

        public void SendMessageEnquued_EXPERIMENTAL(object payload, SerializationFormat format = SerializationFormat.JSON)
        {
            MessagesToClient.Enqueue(payload);
        }
    }
}
