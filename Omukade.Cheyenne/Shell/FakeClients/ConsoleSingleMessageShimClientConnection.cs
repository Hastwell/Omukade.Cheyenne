using Omukade.Cheyenne.ClientConnections;
using Platform.Sdk;
using System.Collections.Concurrent;

namespace Omukade.Cheyenne.Shell.FakeClients
{
    public class ConsoleSingleMessageShimClientConnection<TResponseType> : IClientConnection
    {
        public TResponseType? Message;
        public ManualResetEvent WaitEvent = new ManualResetEvent(false);

        public PlayerMetadata? Tag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsOpen => true;

        public void DisconnectClientImmediately()
        {

        }

        public void SendMessageEnquued_EXPERIMENTAL(object payload, SerializationFormat format = SerializationFormat.JSON)
        {
            if (payload is TResponseType)
            {
                Message = (TResponseType)payload;
                WaitEvent.Set();
            }
            else if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            else
            {
                throw new ArgumentException($"Console command got unexpected response type (expected {typeof(TResponseType).Name}, got {payload.GetType().Name})", nameof(payload));
            }
        }
    }
}
