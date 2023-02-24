namespace Omukade.Cheyenne.Shell.Model
{
    public class GetOnlinePlayersResponse
    {
        public List<OnlinePlayerInfo> OnlinePlayers;

        public record struct OnlinePlayerInfo
        {
            public string? DisplayName;
            public string? PlayerId;
            public string? CurrentGameId;
        }
    }
}
