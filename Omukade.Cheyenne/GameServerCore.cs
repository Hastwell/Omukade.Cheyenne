/*************************************************************************
* Omukade Cheyenne - A PTCGL "Rainier" Standalone Server
* (c) 2022 Hastwell/Electrosheep Networks 
* 
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU Affero General Public License as published
* by the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
* 
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU Affero General Public License for more details.
* 
* You should have received a copy of the GNU Affero General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
**************************************************************************/

using MatchLogic;
using Newtonsoft.Json;
using Omukade.Cheyenne.CustomMessages;
using Omukade.Cheyenne.Patching;
using Platform.Sdk.Models;
using Platform.Sdk.Models.GameServer;
using Platform.Sdk.Models.Matchmaking;
using Platform.Sdk.Models.Query;
using RainierClientSDK;
using SharedLogicUtils.DataTypes;
using SharedLogicUtils.source.Services.Query.Contexts;
using SharedSDKUtils;

namespace Omukade.Cheyenne
{
    public class GameServerCore
    {
        static GameServerCore()
        {
            RainierServiceLogger.instance = new RainierServiceLogger(RainierServiceLogger.LogLevel.WARNING | RainierServiceLogger.LogLevel.ERROR, _ => RainierServiceLogger.Clear());
        }

        static bool rainerAlreadyPatched;
        public static bool GameRulesAlreadyLoaded => precompressedGameRules != null;

        static readonly JsonSerializerSettings jsonResolverToDealWithEnums = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = DeserializeResolver.settings.ContractResolver,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            Converters = new List<JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter(new Newtonsoft.Json.Serialization.DefaultNamingStrategy(), allowIntegerValues: true) }
        };

        static readonly Dictionary<string, bool> FEATURE_FLAGS_TO_USE = new Dictionary<string, bool>
        {
            {"RuleChanges2023", true } // Enables SV behavior for Pokemon Tools as seperate type of trainer vs pre-SV "Tools are also Items"
        };

        private static readonly byte[] DUMMY_EMPTY_SIGNATURE = Array.Empty<byte>();
        private static byte[] precompressedGameRules;
        const int PLAYER_ONE = 0, PLAYER_TWO = 1;

        public ConfigSettings config { get; init; }
        public event Action<string, Exception?>? ErrorHandler;

        internal Dictionary<string, PlayerMetadata> UserMetadata = new Dictionary<string, PlayerMetadata>(2);

        internal Dictionary<string, GameState> ActiveGamesById = new Dictionary<string, GameState>(10);
        Queue<string> PlayersInQueue = new Queue<string>(2);

        public GameServerCore(ConfigSettings settings)
        {
            OfflineAdapterHax.parentInstance = this;
            this.config = settings;
        }

        public static void PatchRainier()
        {
            if (rainerAlreadyPatched) return;
            HarmonyLib.Harmony harmony = new("OmukadeCheyenne");
            harmony.PatchAll();

            rainerAlreadyPatched = true;
        }
        
        public static void RefreshSharedGameRules(ConfigSettings settings)
        {
            GameDataCacheMessage gameDataCache = GetBakedGameData(settings);
            GameDataCache.Clear();
            GameDataCache.AddCachedObjects(gameDataCache);
            precompressedGameRules = MessageExtensions.PrecompressObject(gameDataCache);
        }

        private static void EnsurePlayerDataForMatch(PlayerMetadata playerData)
        {
            if (playerData.CurrentDeck == null)
            {
                throw new InvalidOperationException("Cannot start or propose a game; decklist not sent yet.");
            }
            if (playerData.PlayerOutfit == null)
            {
                throw new InvalidOperationException("Cannot start or propose a game; player outfit not sent yet.");
            }
        }

        static GameDataCacheMessage GetBakedGameData(ConfigSettings config)
        {
            GameDataCacheMessage gdcm = JsonConvert.DeserializeObject<GameDataCacheMessage>(File.ReadAllText(Path.Combine(config.CardDataDirectory, $"game-data-{nameof(GameMode.Standard)}.json")), DeserializeResolver.settings)!;
            return gdcm;
        }

        internal static void SendPacketToClient(PlayerMetadata? client, object payload, bool isPrecompressed = false)
        {
            if (client == null)
            {
                Console.Error.WriteLine("Can't send message to client - client is null (probably already closed)");
                return;
            }

            if (client.PlayerConnectionHelens != null)
            {
                client.PlayerConnectionHelens.SendMessageEnquued_EXPERIMENTAL(payload, Platform.Sdk.SerializationFormat.JSON);
            }
        }

        public void HandleSupplementalDataMessage(SupplementalDataMessageV2 sdm, PlayerMetadata dataForConnection)
        {
            if (sdm.CurrentRegion != null) dataForConnection.PlayerCurrentRegion = sdm.CurrentRegion;
            if (sdm.PlayerId != null)
            {
                if (sdm.PlayerId == dataForConnection.PlayerId)
                {
                    // ignore retransmissions of same player ID.
                }
                else if (dataForConnection.PlayerId != null)
                {
                    throw new InvalidOperationException("Trying to change player ID of a player that already sent a different player ID.");
                }
                else
                {
                    dataForConnection.PlayerId = sdm.PlayerId;
                    if (UserMetadata.TryGetValue(sdm.PlayerId, out PlayerMetadata existingPlayer))
                    {
                        Console.WriteLine($"WARN: Trampling already-existing player metadata for PID {sdm.PlayerId} - display name {existingPlayer.PlayerDisplayName ?? "[no displayname]"}");
#warning Due to the async nature of websockets, this might get messy.
                        existingPlayer.PlayerConnectionHelens.DisconnectClientImmediately();
                        existingPlayer.PlayerId = null; // FIXME: hack so we don't try to double-dispose of this player.
                    }
                    UserMetadata[sdm.PlayerId] = dataForConnection;
                }
            }
            if (sdm.PlayerDisplayName != null) dataForConnection.PlayerDisplayName = sdm.PlayerDisplayName;
            if (sdm.DeckInformation != null)
            {
                dataForConnection.CurrentDeck = sdm.DeckInformation;
            }
            if (sdm.OutfitInformation != null)
            {
                dataForConnection.PlayerOutfit = sdm.OutfitInformation;
            }
        }

        /// <summary>
        /// Forcibly adds a player's metadata to the list of known players, without needing an SDM message. Used only for unit testing purposes where this information is pre-baked.
        /// </summary>
        /// <param name="dataForConnection"></param>
        public void ForciblyAddMetadataToKnownPlayersList(PlayerMetadata dataForConnection)
        {
            UserMetadata[dataForConnection.PlayerId] = dataForConnection;
        }

        void HandleRainierGameMessage(PlayerMetadata player, GameMessage gm)
        {
            if (player.CurrentGame == null)
            {
                throw new InvalidOperationException("Cannot process received game message; no gamestate associated with user.");
            }
            if (gm.message == null)
            {
                throw new ArgumentNullException("GameMessage payload is null");
            }

            // OfflineAdapter::ReceiveOperation
            ServerMessage? smg = JsonConvert.DeserializeObject<ServerMessage>(System.Text.Encoding.UTF8.GetString(gm.message));

            if (smg == null)
            {
                throw new ArgumentNullException("GameMessage's SMG is null");
            }

            string? rawPayload = smg.compressedValue == null ? null : Compression.Unzip(smg.compressedValue);
            GameState currentGame = player.CurrentGame;

            switch (smg.messageType)
            {
                case MessageType.MatchOperation:
                case MessageType.MatchInput:
                case MessageType.MatchInputUpdate:
                    OfflineAdapter.ReceiveOperation(player.PlayerId, currentGame, smg);
                    break;
                case MessageType.SendEmote:
                    if (rawPayload == null) throw new ArgumentNullException("Emote payload is null");

                    string? emoteName = JsonConvert.DeserializeObject<string>(rawPayload);

                    if (emoteName == null) throw new ArgumentNullException("Cannot send a null emote");

                    foreach (SharedSDKUtils.PlayerInfo playerInGame in currentGame.playerInfos)
                    {
                        // Don't send our own emote to ourselves
                        if (playerInGame.playerID == player.PlayerId) continue;

                        SendPacketToClient(UserMetadata.GetValueOrDefault(playerInGame.playerID), new ServerMessage(MessageType.SendEmote, emoteName, playerInGame.playerID, smg.operationID, currentGame.matchId).AsPlayerMessage());
                    }
                    break;
                default:
                    throw new NotImplementedException($"Unsupported Game Message type received from client :: {smg.messageType}");
            }

            if (currentGame.CurrentOperation.workingBoard.isGameOver)
            {
                foreach (SharedSDKUtils.PlayerInfo playerInGame in currentGame.playerInfos)
                {
                    if (UserMetadata.TryGetValue(playerInGame.playerID, out PlayerMetadata playerInGameMetadata))
                    {
                        playerInGameMetadata.CurrentGame = null;
                        ActiveGamesById.Remove(currentGame.matchId);
                    }
                }
            }
        }

        public void HandleRainerMessage(PlayerMetadata player, object message)
        {
            if (message is BeginMatchmaking)
            {
                EnsurePlayerDataForMatch(player);
                PlayersInQueue.Enqueue(player.PlayerId!);

                if (PlayersInQueue.Count >= 2)
                {
                    StartGameBetweenTwoPlayers(UserMetadata[PlayersInQueue.Dequeue()], UserMetadata[PlayersInQueue.Dequeue()]);
                }
            }
            else if (message is CancelMatchmaking cm)
            {
                RemovePlayerFromAllMatchmaking(player);
                SendPacketToClient(player, new MatchmakingCancelled(cm.txid));
            }
            else if (message is GameMessage gm)
            {
                HandleRainierGameMessage(player, gm);
            }
            else if (message is ProposeDirectMatch pdm)
            {
                EnsurePlayerDataForMatch(player);

                if (!UserMetadata.TryGetValue(pdm.targetAccountId.accountId, out PlayerMetadata targetPlayerData))
                {
                    throw new ArgumentException("Sending a match request to an offline/non-existent player.");
                }

                player.DirectMatchCurrentlySendingTo = pdm.targetAccountId.accountId;
                player.DirectMatchMakingToken = Guid.NewGuid().ToString();
                player.DirectMatchMakingTransactionToken = pdm.txid;
                targetPlayerData.DirectMatchCurrentlyReceivingFrom ??= new HashSet<string>(1);
                targetPlayerData.DirectMatchCurrentlyReceivingFrom.Add(player.PlayerId!);

                // The SignedMatchContext or it's data for the sender doesn't seem to matter, just that the client has one, as it does a null check before allowing the CancelDirectMatch to be sent.
                SignedMatchContext smcForSender = new SignedMatchContext(player.DirectMatchMakingToken, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), matchContext: null, signature: DUMMY_EMPTY_SIGNATURE);
                SendPacketToClient(player, smcForSender);

                FriendDirectMatchContext fdmc = Platform.Sdk.Util.Utils.FromJsonBytes<FriendDirectMatchContext>(pdm.context);
                MatchSharedContext msc = new MatchSharedContext { gameMode = fdmc.gameMode, matchTime = fdmc.matchTime, useAutoSelect = fdmc.useAutoSelect, useMatchTimer = fdmc.useMatchTimer, useOperationTimer = fdmc.useOperationTimer };
                byte[] mscBytes = Platform.Sdk.Util.Utils.ToJsonBytes(msc);
                SendPacketToClient(targetPlayerData, new DirectMatchInvitation(sourceAccountId: player.PlayerId, mmToken: player.DirectMatchMakingToken, issuedAt: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), sharedContext: mscBytes, signature: DUMMY_EMPTY_SIGNATURE));
            }
            else if (message is CancelDirectMatch cdm)
            {
                // I think this can only be sent by the initiating player
                CancelDirectMatchmakingInitatedBy(player);
            }
            else if (message is AcceptDirectMatch adm)
            {
                EnsurePlayerDataForMatch(player);

                if (!UserMetadata.TryGetValue(adm.invitation.sourceAccountId, out PlayerMetadata initiatingPlayerMetadata))
                {
                    throw new ArgumentException("Trying to accept a direct match for an offline player.");
                }

                if (initiatingPlayerMetadata.DirectMatchCurrentlySendingTo != player.PlayerId)
                {
                    throw new ArgumentException("Trying to accept a direct match against a player matchmaking a different/no opponent.");
                }
                if (adm.invitation.mmToken != initiatingPlayerMetadata.DirectMatchMakingToken)
                {
                    throw new ArgumentException("Trying to start a direct match against a player with a different matchmaking token.");
                }

                initiatingPlayerMetadata.DirectMatchMakingTransactionToken = default;
                initiatingPlayerMetadata.DirectMatchMakingToken = default;
                initiatingPlayerMetadata.DirectMatchCurrentlySendingTo = default;
                player.DirectMatchCurrentlyReceivingFrom = default;

                // TODO: send the matchaccepted message to the initiating player
                StartGameBetweenTwoPlayers(initiatingPlayerMetadata, player);
            }
        }

        private void RemovePlayerFromAllMatchmaking(PlayerMetadata playerData, bool skipCheckingIfPlayerIsInQueue = false)
        {
            string? concernedPlayerId = playerData.PlayerId;
            if (concernedPlayerId == null) return;

            if (!skipCheckingIfPlayerIsInQueue && !PlayersInQueue.Contains(concernedPlayerId))
            {
                return;
            }

            PlayersInQueue = new Queue<string>(PlayersInQueue.Where(piq => piq != concernedPlayerId));

            foreach (PlayerMetadata pmd in UserMetadata.Values)
            {
                // Cancel players that received a match from this player
                if (pmd.DirectMatchCurrentlySendingTo == playerData.PlayerId)
                {
                    CancelDirectMatchmakingInitatedBy(pmd);
                }

                // Cancel players initiating a match against this player
                if (playerData.PlayerId != null)
                {
                    pmd.RemovePlayerFromDirectMatchReceivingFrom(playerData.PlayerId);
                }
            }
        }

        private void CancelDirectMatchmakingInitatedBy(PlayerMetadata initiatingPlayer)
        {
            initiatingPlayer.DirectMatchCurrentlySendingTo = null;
            SendPacketToClient(initiatingPlayer, new MatchmakingCancelled(txid: initiatingPlayer.DirectMatchMakingTransactionToken));

            foreach (PlayerMetadata pmd in UserMetadata.Values)
            {
                if (pmd.DirectMatchCurrentlyReceivingFrom != null && pmd.DirectMatchCurrentlyReceivingFrom.Contains(initiatingPlayer.PlayerId!))
                {
                    SendPacketToClient(pmd, new Platform.Sdk.Models.Matchmaking.CancellationToken(initiatingPlayer.DirectMatchMakingToken, issuedAt: default, signature: DUMMY_EMPTY_SIGNATURE));
                    pmd.RemovePlayerFromDirectMatchReceivingFrom(initiatingPlayer.PlayerId!);
                }
            }

            initiatingPlayer.DirectMatchMakingToken = default;
            initiatingPlayer.DirectMatchMakingTransactionToken = default;
        }

        private void StartGameBetweenTwoPlayers(PlayerMetadata playerOneMetadata, PlayerMetadata playerTwoMetadata)
        {
            GameState gameState = new GameState
            {
                matchId = Guid.NewGuid().ToString()
            };

            // By default, the game rules always have Player 1 pick the opening coin flip. To "fix" this, crudely randomize the player order.
            if (!config.DisablePlayerOrderRandomization && DateTime.UtcNow.Millisecond % 2 == 0)
            {
                PlayerMetadata p1scratch = playerOneMetadata;
                playerOneMetadata = playerTwoMetadata;
                playerTwoMetadata = p1scratch;
            }

            gameState.playerInfos[PLAYER_ONE].playerName = playerOneMetadata.PlayerDisplayName;
            gameState.playerInfos[PLAYER_ONE].playerID = playerOneMetadata.PlayerId;
            gameState.playerInfos[PLAYER_ONE].sentPlayerInfo = true;
            gameState.playerInfos[PLAYER_ONE].settings = new PlayerSettings { gameMode = GameMode.Standard, gameplayType = GameplayType.Ranked, useAutoSelect = false, useMatchTimer = false, useOperationTimer = false, matchMode = MatchMode.Standard, matchTime = 1500f };
            gameState.playerInfos[PLAYER_ONE].settings.name = playerOneMetadata.PlayerDisplayName;
            gameState.playerInfos[PLAYER_ONE].settings.outfit = playerOneMetadata.PlayerOutfit;
            DeckInfoHax.ImportMetadata(ref gameState.playerInfos[PLAYER_ONE].settings.deckInfo, playerOneMetadata.CurrentDeck!.Value.metadata, e => throw new Exception($"Error parsing deck for {playerOneMetadata.PlayerDisplayName}: {e}"));
            gameState.playerInfos[PLAYER_ONE].settings.deckInfo.cards = new Dictionary<string, int>(playerOneMetadata.CurrentDeck.Value.items);

            gameState.playerInfos[PLAYER_TWO].playerName = playerTwoMetadata.PlayerDisplayName;
            gameState.playerInfos[PLAYER_TWO].playerID = playerTwoMetadata.PlayerId;
            gameState.playerInfos[PLAYER_TWO].sentPlayerInfo = true;
            gameState.playerInfos[PLAYER_TWO].settings = new PlayerSettings { gameMode = GameMode.Standard, gameplayType = GameplayType.Ranked, useAutoSelect = false, useMatchTimer = false, useOperationTimer = false, matchMode = MatchMode.Standard, matchTime = 1500f };
            gameState.playerInfos[PLAYER_TWO].settings.outfit = playerTwoMetadata.PlayerOutfit;
            DeckInfoHax.ImportMetadata(ref gameState.playerInfos[PLAYER_TWO].settings.deckInfo, playerTwoMetadata.CurrentDeck!.Value.metadata, e => throw new Exception($"Error parsing deck for {playerTwoMetadata.PlayerDisplayName}: {e}"));
            gameState.playerInfos[PLAYER_TWO].settings.name = playerTwoMetadata.PlayerDisplayName;
            gameState.playerInfos[PLAYER_TWO].settings.deckInfo.cards = new Dictionary<string, int>(playerTwoMetadata.CurrentDeck.Value.items);

            ActiveGamesById.Add(gameState.matchId, gameState);
            playerOneMetadata.CurrentGame = gameState;
            playerTwoMetadata.CurrentGame = gameState;

            // Flatten the decklists

            // OfflineAdapter.CreateGame
            HashSet<string> allCardsThatAppear = new();
            List<string> deckListP1 = new(60);
            List<string> deckListP2 = new(60);

            foreach (string card in gameState.playerInfos[PLAYER_ONE].settings.deckInfo.cards.Keys)
            {
                allCardsThatAppear.Add(card);
                int cardCount = gameState.playerInfos[PLAYER_ONE].settings.deckInfo.cards[card];
                for (int i = 0; i < cardCount; i++)
                {
                    deckListP1.Add(card);
                }
            }
            foreach (string card in gameState.playerInfos[PLAYER_TWO].settings.deckInfo.cards.Keys)
            {
                allCardsThatAppear.Add(card);
                int cardCount = gameState.playerInfos[PLAYER_TWO].settings.deckInfo.cards[card];
                for (int i = 0; i < cardCount; i++)
                {
                    deckListP2.Add(card);
                }
            }

            // Prepare Caches
            List<CardSource> allCardData = GetCardData(allCardsThatAppear);
            CardCache.Add(allCardData);
            byte[] prebakedCardData = MessageExtensions.PrecompressObject(allCardData);

            MatchOperation bootstrapOperation = new MatchOperation(gameState.matchId, gameMode: GameMode.Standard, GameplayType.Offline,
            gameState.playerInfos[PLAYER_ONE].playerID, gameState.playerInfos[PLAYER_ONE].playerName,
            deckListP1.ToArray(), 1500f, p1UseMatchTimer: gameState.playerInfos[PLAYER_ONE].settings.useMatchTimer, p1UseOperationTimer: gameState.playerInfos[PLAYER_ONE].settings.useOperationTimer, gameState.playerInfos[PLAYER_ONE].settings.useAutoSelect,

            gameState.playerInfos[PLAYER_TWO].playerID, gameState.playerInfos[PLAYER_TWO].playerName,
            deckListP2.ToArray(), 1500f, p2UseMatchTimer: gameState.playerInfos[PLAYER_TWO].settings.useMatchTimer, p2UseOperationTimer: gameState.playerInfos[PLAYER_TWO].settings.useOperationTimer, gameState.playerInfos[PLAYER_TWO].settings.useAutoSelect,
            MatchMode.Standard, prizeCount: 6, debug: gameState.CanUseDebug,
            featureFlags: FEATURE_FLAGS_TO_USE);

            bootstrapOperation.QueuePlayerOperation();
            bootstrapOperation.Handle();
            gameState.CurrentOperation = bootstrapOperation;
            gameState.PreviousOperations.Add(bootstrapOperation);

            // Send JoinGame to each player
            foreach (SharedSDKUtils.PlayerInfo currentPlayerInfo in gameState.playerInfos)
            {
                PlayerMetadata currentPlayerMetadata = currentPlayerInfo.playerID == playerOneMetadata.PlayerId ? playerOneMetadata : playerTwoMetadata;
                SendPacketToClient(currentPlayerMetadata, new JoinGame(txid: default, mmToken: default, region: UserMetadata[currentPlayerInfo.playerID].PlayerCurrentRegion, ticket: gameState.matchId));

                // Sent by official servers, but I have no idea what it does, and I don't think the client even listens to these messages.
                SendPacketToClient(currentPlayerMetadata, new QueryMessage { queryId = "set-enter-match" });

                MatchCreated matchCreated = new MatchCreated()
                {
                    gameDataBuildDate = "todo",
                    cardDataBuildDate = "todo",
                    matchId = gameState.matchId,
                    players = gameState.playerInfos.Select(
                    pi => new PlayerDetails(playerId: pi.playerID, playerEntityId: gameState.GetPlayerEntityId(pi.playerID), playerName: pi.playerName, pi.playerExp, pi.settings.deckInfo, pi.settings.outfit)).ToArray(),
                    readyUpTimeout = 60,
                    offlineMatch = false,
                    useAI = false,
                    clearCache = true,
                    featureSet = FEATURE_FLAGS_TO_USE
                };

                ServerMessage prebakedRulesMessage = new ServerMessage(MessageType.GameData, string.Empty, currentPlayerInfo.playerID, bootstrapOperation.operationID, gameState.matchId);
                prebakedRulesMessage.SetPrecompressedBody(precompressedGameRules);

                ServerMessage prebakedCardCacheMessage = new ServerMessage(MessageType.CardCache, string.Empty, currentPlayerInfo.playerID, bootstrapOperation.operationID, gameState.matchId);
                prebakedCardCacheMessage.SetPrecompressedBody(prebakedCardData);

                SendPacketToClient(currentPlayerMetadata, new ServerMessage(MessageType.MatchCreated, matchCreated, "SERVER", bootstrapOperation.operationID, gameState.matchId).AsPlayerMessage());
                SendPacketToClient(currentPlayerMetadata, prebakedRulesMessage.AsPlayerMessage(), isPrecompressed: true);
                SendPacketToClient(currentPlayerMetadata, prebakedCardCacheMessage.AsPlayerMessage(), isPrecompressed: true);
            }

            ResolveOperation(gameState, bootstrapOperation);
        }

        private static ServerMessage CreatePlayerSelectionHelper(MatchOperation mo, PlayerSelection ps, string playerId, GameState state)
        {
            bool forPlayer1 = ps.ForPlayer1();
            if ((forPlayer1 && mo.workingBoard.player1.ownerPlayerId == playerId) || (!forPlayer1 && mo.workingBoard.player2.ownerPlayerId == playerId))
            {
                PlayerSelectionInfo psi = new PlayerSelectionInfo(ps.CopySelection(), mo, isOffline: false);
                return new ServerMessage(MessageType.MatchInput, psi, playerId, mo.operationID, mo.matchID);
            }
            else
            {
                OpponentSelectionInfo osi = new OpponentSelectionInfo(ps, mo, isOffline: false);
                return new ServerMessage(MessageType.OPMatchInput, osi, playerId, mo.operationID, mo.matchID);
            }
        }

        internal bool ResolveOperation(GameState currentGameState, MatchOperation currentOperation, bool isInputUpdate = false)
        {
            switch (currentOperation.status)
            {
                case OperationStatus.Error:
                    string invalidOperationErrorMessage = $"Error performing operation {currentOperation.operationID} on game {currentGameState.matchId} ({string.Join(" vs ", currentGameState.playerInfos.Select(p => p.playerName))})";
                    Logging.WriteError(invalidOperationErrorMessage);
                    ErrorHandler?.Invoke(invalidOperationErrorMessage, null);
                    //throw new InvalidOperationException();
                    return false;

                case OperationStatus.WaitingForInput:
                    MatchOperationResult morToSendToClient = new MatchOperationResult(currentOperation, deltaIndex: currentOperation.lastSentDeltaIndex, isInputUpdate: isInputUpdate);

                    foreach (SharedSDKUtils.PlayerInfo currentPlayer in currentGameState.playerInfos)
                    {
                        ServerMessage morSmg = new ServerMessage(MessageType.MatchOperation, morToSendToClient, currentPlayer.playerID, morToSendToClient.operationID, currentGameState.matchId);
                        PlayerMessage morPmg = morSmg.AsPlayerMessage();
                        SendPacketToClient(UserMetadata.GetValueOrDefault(currentPlayer.playerID), morPmg);

                        ServerMessage psiSmg = CreatePlayerSelectionHelper(currentOperation, currentOperation.GetPreviousSelection(), currentPlayer.playerID, currentGameState);
                        PlayerMessage psiPmg = psiSmg.AsPlayerMessage();
                        SendPacketToClient(UserMetadata.GetValueOrDefault(currentPlayer.playerID), psiPmg);
                    }

                    currentOperation.lastSentDeltaIndex = currentOperation.actionModifications.Count;
                    return true;
                default:
                    MatchOperationResult mor = new MatchOperationResult(currentOperation, deltaIndex: currentOperation.lastSentDeltaIndex, isInputUpdate: isInputUpdate);
                    byte[] precompressedMor = MessageExtensions.PrecompressObject(mor, compressionLevel: 9);

                    /*bool gameOver = false;
                    // IDK which one is the authoritative "game over", so just try them all. So far, all 3 have been tripped when a game is over.
                    if(mor.actionModifications?.Any(a => a is EndGameModification) == true)
                    {
                        Console.WriteLine($"Game Over (by Contains EGM) - {currentOperation.matchID}");
                        gameOver = true;
                    }
                    if(currentOperation.workingBoard.isGameOver)
                    {
                        Console.WriteLine($"Game Over (by isGameOver property) - {currentOperation.matchID}");
                        gameOver = true;
                    }
                    if(currentOperation.workingBoard.IsGameOver())
                    {
                        Console.WriteLine($"Game Over (by IsGameOver method) - {currentOperation.matchID}");
                        gameOver = true;
                    }*/

                    foreach (SharedSDKUtils.PlayerInfo currentPlayer in currentGameState.playerInfos)
                    {
                        ServerMessage smg = new ServerMessage(MessageType.MatchOperation, string.Empty, currentPlayer.playerID, mor.operationID, currentGameState.matchId);
                        smg.compressedValue = precompressedMor;

                        PlayerMessage pmg = smg.AsPlayerMessage();
                        SendPacketToClient(UserMetadata.GetValueOrDefault(currentPlayer.playerID), pmg, isPrecompressed: true);
                    }

                    currentOperation.lastSentDeltaIndex = currentOperation.actionModifications.Count;
                    return true;
            }
        }

        public void PurgePlayerFromActivePlayers(PlayerMetadata playerData)
        {
            Console.WriteLine($"Player {playerData.PlayerDisplayName ?? "[no displayname]"} disconnected. Is in game: {playerData.CurrentGame != null}");

            // If the player is in a battle, forfit.
            if (playerData.CurrentGame != null)
            {
                string originPlayerEntityId = playerData.CurrentGame.GetPlayerEntityId(playerData.PlayerId!);
                PlayerOperation pop = new PlayerOperation(OperationType.TimeoutForceQuit, playerData.CurrentGame.matchId, originEntityID: originPlayerEntityId, actionGuid: "", targetID: "", BoardPos.Board, playerData.PlayerId);
                ServerMessage smg = new ServerMessage(MessageType.MatchOperation, pop, playerData.PlayerId, pop.operationID, playerData.CurrentGame.matchId);
                OfflineAdapter.ReceiveOperation(playerData.PlayerId, playerData.CurrentGame, smg);
            }

            RemovePlayerFromAllMatchmaking(playerData);
            UserMetadata.Remove(playerData.PlayerId!);
        }

        private List<CardSource> GetCardData(IEnumerable<string> cardsToFetch)
        {
            return cardsToFetch.Select(cardname =>
            {
                if (config.CardSourceOverridesEnable)
                {
                    string overrideFname = Path.Combine(config.CardSourceOverridesDirectory, cardname + ".json");
                    if (File.Exists(overrideFname))
                    {
                        Console.WriteLine("Using found CardData override for " + cardname);
                        return JsonConvert.DeserializeObject<CardSource>(File.ReadAllText(overrideFname), jsonResolverToDealWithEnums)!;
                    }
                }

                string setFolder = cardname.Split(new char[] { '_' }, 2)[0];
                string cardDataFilename = Path.Combine(config.CardDataDirectory, setFolder, cardname + ".json");
                if (!File.Exists(cardDataFilename))
                {
                    throw new ArgumentException($"Cannot fetch card data for card that doesn't exist - {cardname}", nameof(cardsToFetch));
                }
                string cardDataContents = File.ReadAllText(cardDataFilename);
                return JsonConvert.DeserializeObject<CardSource>(cardDataContents, DeserializeResolver.settings)!;
            })
            .ToList();
        }
    }
}
