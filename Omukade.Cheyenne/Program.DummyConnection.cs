using Omukade.Cheyenne.ClientConnections;

namespace Omukade.Cheyenne
{
    partial class Program
    {
        internal static void SendDummyMessageToServer(IClientConnection connection, object payload) => ProcessSingleWsMessage(new Miniserver.Model.ReceivedMessage { Message = payload, ReceivedFrom = connection });
    }
}
