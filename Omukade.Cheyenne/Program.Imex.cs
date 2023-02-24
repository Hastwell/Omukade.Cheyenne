#if !NOIMEX
using System.Runtime.CompilerServices;
using ImexV.Core.Messages.InternalMessages.RainierShim;
using ImexV.Core.Session;
using MatchLogic;
using Newtonsoft.Json;
using Platform.Sdk.Models.GameServer;
using Platform.Sdk.Models.Matchmaking;
using Platform.Sdk.Models.Query;
using RainierClientSDK;
using SharedLogicUtils.DataTypes;
using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedLogicUtils.source.Services.Query.Contexts;
using ImexV.Core.Messages;
using ImexV.Core.Messages.Json.Gameplay;
using System.Security.Cryptography;
using Omukade.Cheyenne.Patching;
using Platform.Sdk.Models;

[assembly: InternalsVisibleTo("Omukade.Cheyenne.PerformanceTesting")]

namespace Omukade.Cheyenne
{
    internal partial class Program
    {
        static Thread imexThread;
        static internal WargServer wargServer;

        static void SpawnImex()
        {
            Console.WriteLine("Spawning IMEX V server...");
            wargServer = new ImexV.Core.Session.WargServer();
            wargServer.ServerPort = config.ImexPort;
            wargServer.ListenForIncomingClients = true;
            wargServer.RespondToPings = true;
            wargServer.NewClientConnected += WargServer_NewClientConnected;
            wargServer.ConnectionClosed += WargServer_ConnectionClosed;
            wargServer.PacketReceived += WargServer_PacketReceived;
            wargServer.ClientNetworkError += WargServer_ClientNetworkError;

            if (config.FakePlayersToHaveOnline != null)
            {
                foreach (string fakePlayerId in config.FakePlayersToHaveOnline)
                {
                    DummyWargSocket fakeSocket = new DummyWargSocket();
                    fakeSocket.ReceiveBuffer.Enqueue(new SupplementalDataMessage { CurrentRegion = "abc", DeckInformationJson = "{}", OutfitInformationJson = "{}", PlayerDisplayName = "Fake Player", PlayerId = fakePlayerId });
                    wargServer.connections.Add(fakeSocket);
                    WargServer_NewClientConnected(fakeSocket);
                }
            }

            imexThread = new Thread(wargServer.ServerThread);
            imexThread.Start();
        }

        private static void WargServer_ClientNetworkError(BaseWargSocket conn, Exception e)
        {
            if (e is ImexV.Core.IllegalPacketReceivedException) return;

            PlayerMetadata? connectedPlayerData = conn?.Tag as PlayerMetadata;

            Console.Error.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error while processing client data for Player {connectedPlayerData?.PlayerDisplayName ?? "[unknown]"}: {e.Message ?? e.GetType().Name}");
            if(connectedPlayerData?.CurrentGame != null)
            {
                string opponentName = connectedPlayerData.CurrentGame.playerInfos.FirstOrDefault(p => p.playerID != connectedPlayerData.PlayerId)?.playerName ?? "[unknown]";
                Console.Error.WriteLine($"Current battle: {connectedPlayerData.CurrentGame.matchId} against {opponentName}");
            }
            else
            {
                Console.Error.WriteLine($"Current battle: [none]");
            }
            Console.Error.WriteLine(e.StackTrace);

            if(config.EnableDiscordErrorWebhook && config.DiscordErrorWebhookUrl != null)
            {
                StringBuilder discordMessageBuilder = new StringBuilder($"Error while processing client data for Player {connectedPlayerData?.PlayerDisplayName ?? "[unknown]"}: {e.Message ?? e.GetType().Name}\n");

                if (connectedPlayerData?.CurrentGame != null)
                {
                    string opponentName = connectedPlayerData.CurrentGame.playerInfos.FirstOrDefault(p => p.playerID != connectedPlayerData.PlayerId)?.playerName ?? "[unknown]";
                    discordMessageBuilder.AppendLine($"Current battle: {connectedPlayerData.CurrentGame.matchId} against {opponentName}");
                }
                else
                {
                    discordMessageBuilder.AppendLine($"Current battle: [none]");
                }

                discordMessageBuilder.Append("```");
                discordMessageBuilder.Append(e.StackTrace);
                discordMessageBuilder.Append("```");

                SendDiscordAlert(discordMessageBuilder.ToString(), config.DiscordErrorWebhookUrl);
            }
        }

        private static void WargServer_ConnectionClosed(BaseWargSocket conn)
        {
            PlayerMetadata playerData = (PlayerMetadata) conn.Tag;
            if (playerData.PlayerId == null)
            {
                return;
            }

            PurgePlayerFromActivePlayers(playerData);
        }

        private static void WargServer_NewClientConnected(ImexV.Core.Session.BaseWargSocket newConnection)
        {
            newConnection.Tag = new PlayerMetadata() { PlayerConnectionImex = newConnection };
        }

        private static void WargServer_PacketReceived(ImexV.Core.PacketReceivedEvent packetEvent)
        {
            PlayerMetadata dataForConnection = (PlayerMetadata)packetEvent.Connection.Tag;

            if(packetEvent.Packet is RainierWrappedJsonFrame rwjf)
            {
                if(rwjf.JsonPayload == null)
                {
                    throw new ArgumentNullException("RWJ Frame contains a null payload.");
                }

                object? payload = JsonConvert.DeserializeObject<object>(rwjf.JsonPayload, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = RainerKnownTypesBinder.INSTANCE });
                if(payload == null)
                {
                    throw new ArgumentNullException("RWJ Frame payload decoded to null.");
                }

                if(dataForConnection.PlayerId == null)
                {
                    throw new InvalidOperationException("RWJ Frame received before any player ID information; cannot process.");
                }

                HandleRainerMessage(dataForConnection, payload);
            }
            else if (packetEvent.Packet is SupplementalDataMessage sdm)
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
                        if(UserMetadata.TryGetValue(sdm.PlayerId, out PlayerMetadata existingPlayer))
                        {
                            Console.WriteLine($"WARN: Trampling already-existing player metadata for PID {sdm.PlayerId} - display name {existingPlayer.PlayerDisplayName ?? "[no displayname]"}");
                            wargServer.DisconnectClientImmediately(existingPlayer.PlayerConnectionImex);
                            existingPlayer.PlayerId = null; // FIXME: hack so we don't try to double-dispose of this player.
                        }
                        UserMetadata.Add(sdm.PlayerId, dataForConnection);
                    }
                }
                if (sdm.PlayerDisplayName != null) dataForConnection.PlayerDisplayName = sdm.PlayerDisplayName;
                if (sdm.DeckInformationJson != null)
                {
                    dataForConnection.CurrentDeck = JsonConvert.DeserializeObject<CollectionData>(sdm.DeckInformationJson);
                }
                if (sdm.OutfitInformationJson != null)
                {
                    dataForConnection.PlayerOutfit = JsonConvert.DeserializeObject<Outfit>(sdm.OutfitInformationJson);
                }
            }
            else if (packetEvent.Packet is GetOnlineFriends gof)
            {
                OnlineFriendsResponse ofr;
                if (gof.FriendIds == null || gof.FriendIds.Count == 0)
                {
                    ofr = new OnlineFriendsResponse { CurrentlyOnlineFriends = new List<string>(0), TransactionId = gof.TransactionId };
                }
                else
                {
                    List<string> matchingOnlinePlayers = gof.FriendIds.Where(UserMetadata.ContainsKey).ToList();
                    ofr = new OnlineFriendsResponse { CurrentlyOnlineFriends = matchingOnlinePlayers, TransactionId = gof.TransactionId };
                }
                // Console.WriteLine($"Checking for friends {JsonConvert.SerializeObject(gof.FriendIds)} ; returning {JsonConvert.SerializeObject(ofr.CurrentlyOnlineFriends)}");
                packetEvent.Connection.TransmitQueue.Enqueue(ofr);
            }
        }
    }
}
#endif