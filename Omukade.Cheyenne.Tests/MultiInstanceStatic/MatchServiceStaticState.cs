using ClientNetworking.Models.GameServer;
using RainierClientSDK;

namespace Omukade.Cheyenne.Tests.MultiInstanceStatic
{
    public struct MatchServiceStaticState : IStateApplier
    {
        public string _gameID;
        public List<MatchInfo> activeMatches;
        // public Queue<PlayerMessage> _playerMessages;

        public void ApplyState()
        {
            MatchService.activeMatches = this.activeMatches;
            MatchService._gameID = this._gameID;
        }

        public void InstantiateNewReferences()
        {
            MatchService.activeMatches = new();
        }

        public void SaveState()
        {
            this.activeMatches = MatchService.activeMatches;
            this._gameID = MatchService._gameID;
        }
    }
}
