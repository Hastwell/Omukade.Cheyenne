using Omukade.Cheyenne.ClientConnections;
using Omukade.Cheyenne.Miniserver.Controllers;

namespace Omukade.Cheyenne.Miniserver.Model
{
    public struct ReceivedMessage
    {
        public IClientConnection ReceivedFrom;
        public object Message;
    }
}
