using Platform.Sdk.Stomp;

namespace Omukade.Cheyenne.Miniserver.Model
{
    public struct StompFrameWithData
    {
        public StompFrame frameHeaders;
        public byte[] payload;
    }
}
