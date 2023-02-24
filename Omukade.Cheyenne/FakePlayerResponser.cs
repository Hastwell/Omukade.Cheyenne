using MatchLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    internal static class FakePlayerResponser
    {
        static GamePhases currentPhase = GamePhases.CallOpeningCoinFlip;
        enum GamePhases
        {
            CallOpeningCoinFlip,
            CallFirstOrSecond,
            ChooseOpeningPokemonP2PlayMinccinio,
            ChooseOpeningPokemonP2Finished,
            DONOTHING_WaitForP2PokemonToResolve,
            ChooseOpeningPokemonP1PlayCresselia,
            ChooseOpeningPokemonP1Finished,
            TakeMulligansP2,
        }

        internal static void ResolvePlayer1(MatchOperation currentOperation, object playerSelection)
        {
            if (playerSelection is OpponentSelectionInfo) return;
            PlayerSelectionInfo psi = (PlayerSelectionInfo)playerSelection;

            Console.WriteLine($"Requesting Player Interaction for {currentOperation.operationID}");

            switch(currentPhase)
            {
                case GamePhases.CallOpeningCoinFlip:
                    Console.WriteLine("Calling heads on opening coinflip");
                    ((TextSelection) psi.playerSelection.variableSelection).selectedIndex = 0;
                    currentPhase++;

                    SendMatchInput(psi.playerSelection);
                    return;
                case GamePhases.CallFirstOrSecond:
                    Console.WriteLine("Choosing to go first");
                    ((TextSelection)psi.playerSelection.variableSelection).selectedIndex = 0;
                    currentPhase++;

                    SendMatchInput(psi.playerSelection);

                    return;
                default:
                    throw new InvalidOperationException("Attempting to resolve a P1 action that I don't know how to handle");
            }
        }

        internal static void ResolvePlayer2(MatchOperation currentOperation, object playerSelection)
        {
            if (playerSelection is OpponentSelectionInfo) return;
            PlayerSelectionInfo psi = (PlayerSelectionInfo)playerSelection;

            Console.WriteLine($"Requesting Opponent Interaction for {currentOperation.operationID}");

            switch (currentPhase)
            {
                case GamePhases.TakeMulligansP2:
                    ((TextSelection)psi.playerSelection.variableSelection).selectedIndex = 1; // Take 1 mulligan card
                    currentPhase++;

                    Console.WriteLine("Taking P2 Mulligan [x1 card taken]");

                    SendMatchInput(psi.playerSelection);
                    return;
                default:
                    throw new InvalidOperationException("Attempting to resolve a P2 action that I don't know how to handle");
            }
        }

        internal static void ResolveMatchOperationResult(MatchOperationResult mor)
        {
            // TODO THEORY: check for placable entities, as in salmon

            //var allMatchEntities = Program.globalGameState.CurrentOperation.workingBoard.AllEntityList();
            // Attempt to determine playable entities
            AvailableActionEvaluator aae = new AvailableActionEvaluator { gameState = Program.globalGameState };

            // Silence the rainier logging, as all these operations are REALLY, REALLY verbose!
            Patching.RainierServiceLoggerLogEverything.BE_QUIET = true;
            var playableActivePokemon_P1 = aae.CardsThatCanBePlacedInPosition(BoardPos.Player1Active, Program.MY_PLAYER_ID);
            var playableBenchedPokemon_P1 = aae.CardsThatCanBePlacedInPosition(BoardPos.Player1Bench, Program.MY_PLAYER_ID);
            var playableToField_P1 = aae.CardsThatCanBePlacedInPosition(BoardPos.Board, Program.MY_PLAYER_ID);

            var playableActivePokemon_P2 = aae.CardsThatCanBePlacedInPosition(BoardPos.Player2Active, Program.OPPONENT_PLAYER_ID);
            var playableBenchPokemon_P2 = aae.CardsThatCanBePlacedInPosition(BoardPos.Player2Bench, Program.OPPONENT_PLAYER_ID);
            var playableToField_P2 = aae.CardsThatCanBePlacedInPosition(BoardPos.Board, Program.OPPONENT_PLAYER_ID);
            bool canEndTurn_P1 = aae.CanEndTurn(Program.MY_PLAYER_ID);
            bool canEndTurn_P2 = aae.CanEndTurn(Program.OPPONENT_PLAYER_ID);
            Patching.RainierServiceLoggerLogEverything.BE_QUIET = false;

            /*var allPlacableCards = allMatchEntities.Where(ent => ent.placeableBoardPos.Any()).ToList();
            var allCardsThatCanUseSomething = allMatchEntities.Where(ent => ent.activeUsableActions.Any()).ToList();
            var allCardsThatCanAttachToSomething = allMatchEntities.Where(ent => ent.attachableEntities.Any()).ToList();*/

            switch (currentPhase)
            {
                case GamePhases.ChooseOpeningPokemonP2PlayMinccinio:
                { 
                    // P1 has no basic; P2 has a Minccino
                    Console.WriteLine("Need to choose a card for P2 opening basic; choices are:");
                    ShowCardsInLocation(BoardPos.Player2Hand);

                    CardEntity openingMinccino = FirstCardWithName(BoardPos.Player2Hand, "Minccino");
                    PlayerOperation placeP2minccino = new(OperationType.Place, Program.globalGameState.matchId, openingMinccino.entityID, actionGuid: "", targetID: "", BoardPos.Player2Active, Program.OPPONENT_PLAYER_ID);
                    currentPhase++;
                    SendMatchOperation(placeP2minccino);

                    // wait for the operation to fully resolve before acting on the new mincinno; otherwise the game acts as though you haven't played it yet.
                    Console.WriteLine("Choosing not to play benched Pokemon (though I don't have any)");
                    PlayerOperation allDone1 = new(OperationType.End, Program.globalGameState.matchId, originEntityID: PlayerWithId(Program.OPPONENT_PLAYER_ID).entityID, actionGuid: "", targetID: "", BoardPos.Board, Program.OPPONENT_PLAYER_ID);
                    currentPhase++;
                    SendMatchOperation(allDone1);

                    CardEntity openingCresselia = FirstCardWithName(BoardPos.Player1Hand, "Cresselia");
                    PlayerOperation placeP1cresselia = new(OperationType.Place, Program.globalGameState.matchId, openingCresselia.entityID, actionGuid: "", targetID: "", BoardPos.Player1Active, Program.MY_PLAYER_ID);
                    currentPhase++;
                    SendMatchOperation(placeP1cresselia);

                    PlayerOperation allDone2 = new(OperationType.End, Program.globalGameState.matchId, originEntityID: PlayerWithId(Program.MY_PLAYER_ID).entityID, actionGuid: "", targetID: "", BoardPos.Board, Program.MY_PLAYER_ID);
                    currentPhase++;
                    SendMatchOperation(allDone2);

                        break;
                }
                case GamePhases.DONOTHING_WaitForP2PokemonToResolve:
                    currentPhase++;
                    break;
                case GamePhases.ChooseOpeningPokemonP1PlayCresselia:
                {



                    /*Console.WriteLine("Need to choose a card for P1 opening basic; choices are:");
                    ShowCardsInLocation(BoardPos.Player1Hand);

                    // Done to shuffle away P1's empty hand
                    PlayerOperation allDone1 = new(OperationType.End, Program.globalGameState.matchId, originEntityID: PlayerWithId(Program.MY_PLAYER_ID).entityID, actionGuid: "", targetID: "", BoardPos.Board, Program.MY_PLAYER_ID);
                    currentPhase++;
                    SendMatchOperation(allDone1);*/

                    break;
                }
                default:
                    break;
            }
        }

        static void SendMatchOperation(PlayerOperation po)
        {
            MatchOperation mo = Program.CreateOperation(po);
            Program.HandleOperation(mo);
        }

        static void SendMatchInput(PlayerSelection ps)
        {
            if(!ps.ValidateSelection())
            {
                throw new ArgumentException("Value provided by PlayerSelection did not validate!");
            }

            Program.globalGameState.CurrentOperation.UpdateSelection(ps);
            Program.HandleOperation(Program.globalGameState.CurrentOperation);
        }

        static void SentMatchInputUpdate(PlayerSelection ps)
        {
            throw new NotImplementedException();
        }

        static void ShowCardsInLocation(BoardPos boardPos)
        {
            var allMatchEntities = Program.globalGameState.CurrentOperation.workingBoard.AllEntityList();
            foreach (var matchingCard in allMatchEntities
                .Where(e => e.currentPos == boardPos && e is CardEntity)
                .Select(e => $"[{e.entityID}] - {((CardEntity)e).cardName}"))
            {
                Console.WriteLine(matchingCard);
            }
        }

        static CardEntity FirstCardWithName(BoardPos boardPos, string name)
        {
            return (CardEntity) Program.globalGameState.CurrentOperation.workingBoard.AllEntityList()
                .First(e => e.currentPos == boardPos && e is CardEntity ce && ce.cardName == name);
        }

        static PlayerEntity PlayerWithId(string playerUserId) => (PlayerEntity)Program.globalGameState.CurrentOperation.workingBoard.AllEntityList()
            .First(e => e is PlayerEntity pe && pe.ownerPlayerId == playerUserId);
    }
}
