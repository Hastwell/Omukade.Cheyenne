using MatchLogic;
using Newtonsoft.Json;
using RainierClientSDK;
using SharedSDKUtils;

namespace Omukade.Cheyenne
{
    internal class Program
    {
        public const string MY_PLAYER_NAME = "Cheyenne";
        public const string MY_PLAYER_ID = "id-cheyenne";

        public const string OPPONENT_PLAYER_NAME = "Professor";
        public const string OPPONENT_PLAYER_ID = "professor";

        public const int PLAYER_ONE = 0, PLAYER_TWO = 1;

        internal static GameState globalGameState;

        static void Main(string[] args)
        {
            Console.WriteLine("Omukade Cheyenne");
            Console.WriteLine("(c) 2022 Electrosheep Networks");

            PatchRainier();
            GamePreinit();
            (IEnumerable<string> decklistP1, IEnumerable<string> decklistP2) = GameInit();
            StartGame(decklistP1, decklistP2);
        }

        static void PatchRainier()
        {
            Console.WriteLine("Patching Rainer...");

            var harmony = new HarmonyLib.Harmony("OmukadeCheyenne");
            harmony.PatchAll();
        }

        static void GamePreinit()
        {
            Console.WriteLine("Game Preinit...");

            globalGameState = new GameState
            {
                matchId = Guid.NewGuid().ToString()
            };

            globalGameState.playerInfos[PLAYER_ONE].playerName = MY_PLAYER_NAME;
            globalGameState.playerInfos[PLAYER_ONE].playerID = MY_PLAYER_ID;
            globalGameState.playerInfos[PLAYER_ONE].sentPlayerInfo = true;
            globalGameState.playerInfos[PLAYER_ONE].settings = JsonConvert.DeserializeObject<PlayerSettings>(File.ReadAllText(@"MockData\RequestPlayerCustomizationsResponse.json"))!;

            globalGameState.playerInfos[PLAYER_TWO].playerID = OPPONENT_PLAYER_ID;
            globalGameState.playerInfos[PLAYER_TWO].playerName = OPPONENT_PLAYER_NAME;
            globalGameState.playerInfos[PLAYER_TWO].sentPlayerInfo = true;
            globalGameState.playerInfos[PLAYER_TWO].settings = ParseAiCustomizations();

            // Enable more verbose logging
            RainierServiceLogger.instance = new RainierServiceLogger(RainierServiceLogger.LogLevel.ALL, _ => RainierServiceLogger.instance.logString.Clear());
        }

        static (IEnumerable<string> p1decklist, IEnumerable<string> p2decklist) GameInit()
        {
            Console.WriteLine("Game Init - phase OA.CreateGame");
            // OfflineAdapter.CreateGame
            List<string> list = new List<string>();

            HashSet<string> allCardsThatAppear = new();
            List<string> deckListP1 = new(60);
            List<string> deckListP2 = new(60);

            foreach (string card in globalGameState.playerInfos[PLAYER_ONE].settings.deckInfo.cards.Keys)
            {
                allCardsThatAppear.Add(card);
                int cardCount = globalGameState.playerInfos[PLAYER_ONE].settings.deckInfo.cards[card];
                for (int i = 0; i < cardCount; i++)
                {
                    deckListP1.Add(card);
                }
            }
            foreach (string card in globalGameState.playerInfos[PLAYER_TWO].settings.deckInfo.cards.Keys)
            {
                allCardsThatAppear.Add(card);
                int cardCount = globalGameState.playerInfos[PLAYER_TWO].settings.deckInfo.cards[card];
                for (int i = 0; i < cardCount; i++)
                {
                    deckListP2.Add(card);
                }
            }

            CardCache.Add(GetBakedCachedCards());
            GameDataCache.AddCachedObjects(GetBakedGameData());

            return (deckListP1, deckListP2);
        }

        static PlayerSettings ParseAiCustomizations()
        {
            FlatBuffers.ByteBuffer bb = new(File.ReadAllBytes(@"MockData\offline-get-ai-customizations-response.bin"));
            com.pokemon.studio.contracts.client_gameserver.QueryResponse qrModel = com.pokemon.studio.contracts.client_gameserver.QueryResponse.GetRootAsQueryResponse(bb);
            SharedLogicUtils.source.Services.Query.Responses.OfflineMatchResponse omr = JsonConvert.DeserializeObject<SharedLogicUtils.source.Services.Query.Responses.OfflineMatchResponse>(System.Text.Encoding.UTF8.GetString(qrModel.GetResultBytes()!.Value), DeserializeResolver.settings)!;

            return omr.customization;
        }
        static List<CardSource> GetBakedCachedCards()
        {
            byte[] rawFile = File.ReadAllBytes(@"MockData\offline-get-card-data-cache-response.bin");
            var rawFileSegment = rawFile.AsSpan(24, rawFile.Length - (24 + 3));
            SharedLogicUtils.source.Services.Query.Responses.CardDataResponse cdr = JsonConvert.DeserializeObject<SharedLogicUtils.source.Services.Query.Responses.CardDataResponse>(System.Text.Encoding.UTF8.GetString(rawFileSegment))!;

            List<CardSource> foundCards = JsonConvert.DeserializeObject<List<CardSource>>(cdr.cardData, DeserializeResolver.settings)!;

            return foundCards;
        }
        static GameDataCacheMessage GetBakedGameData()
        {
            GameDataCacheMessage gdcm = JsonConvert.DeserializeObject<GameDataCacheMessage>(File.ReadAllText(@"MockData\game-data.json"), DeserializeResolver.settings)!;

            return gdcm;
        }

        /// <summary>
        /// Starts a pre-initialized game, and returns the root operation.
        /// </summary>
        /// <param name="deckListP1"></param>
        /// <param name="deckListP2"></param>
        /// <returns></returns>
        static MatchOperation StartGame(IEnumerable<string> deckListP1, IEnumerable<string> deckListP2)
        {
            //oaGameState = OfflineAdapter.InitializeGame(MY_PLAYER_NAME, MY_PLAYER_ID, OPPONENT_PLAYER_NAME, OPPONENT_PLAYER_ID);
            MatchOperation hugeAssRootOperation = new MatchOperation(globalGameState.matchId, gameMode: GameMode.Standard, GameplayType.Offline, 
                globalGameState.playerInfos[PLAYER_ONE].playerID, globalGameState.playerInfos[PLAYER_ONE].playerName, 
                deckListP1.ToArray(), 1500f, globalGameState.playerInfos[PLAYER_ONE].settings.useMatchTimer, globalGameState.playerInfos[PLAYER_ONE].settings.useOperationTimer, globalGameState.playerInfos[PLAYER_ONE].settings.useAutoSelect,

                globalGameState.playerInfos[PLAYER_TWO].playerID, globalGameState.playerInfos[PLAYER_TWO].playerName, 
                deckListP2.ToArray(), 1500f, globalGameState.playerInfos[PLAYER_TWO].settings.useMatchTimer, globalGameState.playerInfos[PLAYER_TWO].settings.useOperationTimer, globalGameState.playerInfos[PLAYER_TWO].settings.useAutoSelect,
                MatchMode.Standard, prizeCount: 6, globalGameState.CanUseDebug);

            hugeAssRootOperation.QueuePlayerOperation();
            hugeAssRootOperation.Handle();
            globalGameState.CurrentOperation = hugeAssRootOperation;
            globalGameState.PreviousOperations.Add(hugeAssRootOperation);

            ResolveOperation(hugeAssRootOperation);

            return hugeAssRootOperation;
        }

        static void ResolveOperation(MatchOperation currentOperation)
        {
            switch(currentOperation.status)
            {
                case OperationStatus.Error:
                    Logging.WriteError("Error performing operation");
                    throw new InvalidOperationException();

                case OperationStatus.WaitingForInput:
                    MatchOperationResult morToSendToClient = new MatchOperationResult(currentOperation, deltaIndex: currentOperation.lastSentDeltaIndex, isInputUpdate: false);
                    object playerSelectionToSendToClient = CreatePlayerSelectionEx(globalGameState.playerInfos[PLAYER_ONE].playerID, currentOperation, currentOperation.GetPreviousSelection());
                    object playerSelectionToSendToOpponent = CreatePlayerSelectionEx(globalGameState.playerInfos[PLAYER_TWO].playerID, currentOperation, currentOperation.GetPreviousSelection());

                    //FakePlayerResponser.ResolveMatchOperationResult(morToSendToClient);

                    //FakePlayerResponser.ResolvePlayer1(currentOperation, playerSelectionToSendToClient);
                    //FakePlayerResponser.ResolvePlayer2(currentOperation, playerSelectionToSendToOpponent);

                    currentOperation.lastSentDeltaIndex = currentOperation.actionModifications.Count;

                    // throw new NotImplementedException("Requesting input from players is not yet supported");
                    break;
                default:
                    // TODO: Send operations to players. Eg, as found in OfflineAdapter.ResolveOperation
                    Console.WriteLine($"Operation {currentOperation.operationID} :: status {currentOperation.status}");

                    FakePlayerResponser.ResolveMatchOperationResult(new MatchOperationResult(currentOperation, deltaIndex: currentOperation.lastSentDeltaIndex, isInputUpdate: false));
                    currentOperation.lastSentDeltaIndex = currentOperation.actionModifications.Count;
                    break;
            }
        }

        internal static void HandleOperation(MatchOperation newOperation)
        {
            if (newOperation.workingBoard.isGameOver) return;
            newOperation.Handle();

            // Our ResolveOperation either fully works, or throws exception.
            ResolveOperation(newOperation);
            if(true /*ResolveOperation*/)
            {
                int currentOperationIndex = globalGameState.PreviousOperations.FindIndex(mo => mo.operationID == globalGameState.CurrentOperation.operationID);
                if(currentOperationIndex != -1)
                {
                    globalGameState.PreviousOperations[currentOperationIndex] = globalGameState.CurrentOperation;
                }
                else
                {
                    globalGameState.PreviousOperations.Add(newOperation);
                }

                while (globalGameState.PreviousOperations.Count > 2)
                {
                    globalGameState.PreviousOperations.RemoveAt(0);
                }
                globalGameState.CurrentOperation = newOperation;
            }
        }

        internal static MatchOperation CreateOperation(PlayerOperation po)
        {
            if (globalGameState.CurrentOperation.status != OperationStatus.Finished && po.operationType != OperationType.Quit)
            {
                throw new ArgumentException("Cannot create a new operation while one is still unfinished");
            }

            globalGameState.CurrentOperation.status = OperationStatus.Finished;
            MatchOperation matchOperation = new MatchOperation(po, globalGameState.CurrentOperation);
            matchOperation.QueuePlayerOperation();
            if (matchOperation.status == OperationStatus.Error)
            {
                throw new Exception("Error queueing new operation");
            }
            return matchOperation;
        }

        static object CreatePlayerSelectionEx(string playerId, MatchOperation mo, PlayerSelection previousSelection)
        {
            if ((previousSelection.ForPlayer1() && mo.workingBoard.player1.ownerPlayerId == playerId) || (!previousSelection.ForPlayer1() && mo.workingBoard.player2.ownerPlayerId == playerId))
            {
                PlayerSelectionInfo playerSelectionInfo = new PlayerSelectionInfo(previousSelection.CopySelection(), mo, isOffline: false);
                //LogMsg("Create PlayerSelectionInfo for player:" + playerId + "\n" + playerSelectionInfo.playerSelection.Print(), globalGameState, mo.operationID, messageID, playerId);
                // return new ServerMessage(MessageType.MatchInput, playerSelectionInfo, playerId, mo.operationID, mo.matchID);
                return playerSelectionInfo;
            }
            OpponentSelectionInfo opponentSelectionInfo = new OpponentSelectionInfo(previousSelection, mo, isOffline: false);
            //LogMsg("Create OpponentSelectionInfo for player:" + playerId + "\n" + opponentSelectionInfo.Print(), state, mo.operationID, messageID, playerId);
            //return new ServerMessage(MessageType.OPMatchInput, opponentSelectionInfo, playerId, mo.operationID, mo.matchID);
            return opponentSelectionInfo;
        }
    }
}