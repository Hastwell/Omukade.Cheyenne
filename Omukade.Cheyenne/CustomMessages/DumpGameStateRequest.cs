namespace Omukade.Cheyenne.CustomMessages
{
    public class DumpGameStateRequest
    {
        public DumpGameStateRequest(string gameId)
        {
            this.gameId = gameId;
        }
        public string gameId;
    }
}
