//#define EVALUATE_CARDS_IN_ALL_LOCATIONS

using MatchLogic;
using RainierClientSDK;
using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    internal class AvailableActionEvaluator
    {
        public GameState gameState;

        public bool CanEndTurn(string playerId) => CanPerformPlayerOperation(CreateEndTurnOperation(playerId));
        public bool CanPlaceCard(string fromEntityId, BoardPos targetPos, string playerId) => CanPerformPlayerOperation(CreatePlaceCardOperation(fromEntityId, targetPos, playerId));

        public void GetAllActionsForAllCards(string playerId)
        {
            IEnumerable<CardEntity> allEligibleCards = gameState.CurrentOperation.workingBoard.AllEntityList()
                .Where
                (
                    e => e.ownerPlayerId == playerId && e is CardEntity
#if !EVALUATE_CARDS_IN_ALL_LOCATIONS
                    && !(e.currentPos == BoardPos.Player1Deck || e.currentPos == BoardPos.Player2Deck || e.currentPos == BoardPos.Player1Prize || e.currentPos == BoardPos.Player2Prize || e.currentPos == BoardPos.Player1LostZone || e.currentPos == BoardPos.Player2LostZone)
#endif
                )
                .Cast<CardEntity>();

            foreach(CardEntity ce in allEligibleCards)
            {
                if(ce.IsInPlay())
                {
#warning TODO: find a better way to determine the triggerEvent for StateInformation
                    StateInformation si = new StateInformation(gameState.CurrentOperation, triggerEvent: null, gameState.CurrentOperation.GetCurrentAction());
                    var useActions = ce.GetUseActions(stateInformation: null);
                }
            }
        }

        public List<CardEntity> CardsThatCanBePlacedInPosition(BoardPos targetPos, string playerId)
        {
            return gameState.CurrentOperation.workingBoard.AllEntityList()
                .Where(e => e is CardEntity && CanPlaceCard(e.entityID, targetPos, playerId))
                .Cast<CardEntity>()
                .ToList();
        }

        private bool CanPerformPlayerOperation(PlayerOperation po)
        {
            MatchOperation testOperation = new MatchOperation(gameState.CurrentOperation.workingBoard);
            testOperation.SetPlayerOperation(po);
            return testOperation.CanPerformOperation();
        }

        private PlayerOperation CreateEndTurnOperation(string playerId)
        {
            PlayerEntity pe = PlayerWithId(playerId);
            return new PlayerOperation(OperationType.End, gameState.matchId, pe.entityID, "", "", BoardPos.Board, pe.ownerPlayerId);
        }

        private PlayerOperation CreatePlaceCardOperation(string entityId, BoardPos targetPos, string playerId) => new PlayerOperation(OperationType.Place, gameState.matchId, entityId, actionGuid: "", targetID: "", targetPos, playerId);

        PlayerEntity PlayerWithId(string playerUserId) => (PlayerEntity) gameState.CurrentOperation.workingBoard.AllEntityList()
            .First(e => e is PlayerEntity pe && pe.ownerPlayerId == playerUserId);
    }
}
