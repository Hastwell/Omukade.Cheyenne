namespace Omukade.Cheyenne.Miniserver.ControlMessages
{
    public static class ControlMessages
    {
        public static readonly object CLIENT_DISCONNECTED = ControlMessage.ClientDisconnected;
    }

    public enum ControlMessage
    {
        Unknown,
        ClientDisconnected,
        TestPingPacket,
        TestPongResponse,
    }
}
