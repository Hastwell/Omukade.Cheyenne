using RainierClientSDK.source.Game;
using SharedSDKUtils;

namespace Omukade.Cheyenne.Shell.Model
{
    class GetCurrentGamesResponse
    {
        internal List<GameSummary> ongoingGames;

        internal struct GameSummary
        {
            public string GameId;
            public string Player1, Player2;
            public int PrizeCountPlayer1, PrizeCountPlayer2;
        }
    }
}
