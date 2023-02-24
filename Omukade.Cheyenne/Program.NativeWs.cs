using Omukade.Cheyenne.ClientConnections;
using Omukade.Cheyenne.CustomMessages;
using Omukade.Cheyenne.Miniserver.Controllers;
using Omukade.Cheyenne.Miniserver.ControlMessages;
using Omukade.Cheyenne.Miniserver.Model;
using Omukade.Cheyenne.Shell.Model;
using Platform.Sdk.Models.GameServer;
using Platform.Sdk.Models.Matchmaking;
using Platform.Sdk.Models.Query;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Omukade.Cheyenne
{
    partial class Program
    {
        internal const string REFLECT_MESSAGE_MAGIC = nameof(REFLECT_MESSAGE_MAGIC);
        const bool EnableHttps = false;
        const bool EnableHttp = true;

        /// <summary>
        /// Sets the HTTPS certificate to use when <see cref="EnableHttps"/> is enabled.
        /// </summary>
        public static X509Certificate2 HttpsCertificate { get; set; }

        static WebApplication app;

        internal static Thread? wsMessageProcessorThread;

        internal static WebApplication PrepareWsServer(params Assembly[] additionalAssembliesToScanForControllers) => PrepareWsServer(additionalAssembliesToScanForControllers, enableProxyMiddleware: true);
        internal static WebApplication PrepareWsServer(Assembly[] additionalAssembliesToScanForControllers, bool enableProxyMiddleware = true)
        {
            if (EnableHttps && HttpsCertificate == null)
            {
                throw new InvalidOperationException("Cannot start server; HTTPS is enabled, but no certificate was provided.");
            }

            continueRunningWsServer = true;

            // Prepare controller
            StompController.ClientConnected = new Func<IClientConnection, Task>(Stomp_NewConnection);
            StompController.ClientDisconnected = new Func<IClientConnection, Task>(Stomp_ConnectionClosed);
            StompController.MessageReceived = new Func<IClientConnection, object, Task>(Stomp_MessageReceived);

            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            // Reduce logging verbosity
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Add services to the container.
            Assembly assemblyOfServerCore = Assembly.GetExecutingAssembly();
            var controllerBuilder = builder.Services.AddControllers().AddApplicationPart(assemblyOfServerCore);
            foreach (Assembly assem in additionalAssembliesToScanForControllers)
            {
                controllerBuilder = controllerBuilder.AddApplicationPart(assem);
            }
            controllerBuilder = controllerBuilder.AddControllersAsServices();
            controllerBuilder = controllerBuilder.AddJsonOptions(jsonOptions => jsonOptions.JsonSerializerOptions.IncludeFields = true);

            builder.WebHost.ConfigureKestrel((context, krestrelOptions) =>
            {
                if (EnableHttps)
                {
                    krestrelOptions.ListenAnyIP(443, lo =>
                    {
                        lo.UseHttps(https => https.ServerCertificate = HttpsCertificate);
                    });
                }
                if (EnableHttp)
                {
                    krestrelOptions.ListenAnyIP(config.HttpPort);
                }
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            };
            app.UseWebSockets(webSocketOptions);
            app.MapControllers();

            return app;
        }

        internal static void StartWsProcessorThread()
        {
            if (wsMessageProcessorThread?.ThreadState == ThreadState.Running) throw new InvalidOperationException("Cannot start processor thread; another one is still running.");
            wsMessageProcessorThread = new Thread(WebsocketCheynneThread);
            wsMessageProcessorThread.Start();
        }

        private static Task Stomp_NewConnection(IClientConnection arg)
        {
            arg.Tag = new PlayerMetadata { PlayerConnectionHelens = arg };
            return Task.CompletedTask;
        }

        private static Task Stomp_MessageReceived(IClientConnection controller, object message)
        {
            receiveQueue.Enqueue(new ReceivedMessage { ReceivedFrom = controller, Message = message });
            return Task.CompletedTask;
        }

        private static Task Stomp_ConnectionClosed(IClientConnection controller)
        {
            receiveQueue.Enqueue(new ReceivedMessage { ReceivedFrom = controller, Message = ControlMessages.CLIENT_DISCONNECTED });
            return Task.CompletedTask;
        }

        static ConcurrentQueue<ReceivedMessage> receiveQueue = new ConcurrentQueue<ReceivedMessage>();
        internal static bool continueRunningWsServer = true;

        private static void WebsocketCheynneThread()
        {
            while(continueRunningWsServer)
            {
                // Process backlogged messages
                while (receiveQueue.TryDequeue(out ReceivedMessage messageWrapper))
                {
                    if (!messageWrapper.ReceivedFrom.IsOpen && !(messageWrapper.Message is ControlMessage)) continue;
                    ProcessSingleWsMessage(messageWrapper);
                }

                Thread.Sleep(10 /*ms*/);
            }
        }

        public static async ValueTask StopWsServer()
        {
            if (app == null) return;
            continueRunningWsServer = false;
            await app.StopAsync();
            await app.DisposeAsync();
        }

        static void ProcessSingleWsMessage(ReceivedMessage messageWrapper)
        {
            IClientConnection controller = messageWrapper.ReceivedFrom;
            object message = messageWrapper.Message;

            try
            {
                switch (message)
                {
                    case ControlMessage:
                        ProcessWsControlMessage(controller, message);
                        break;
                    case QueryMessage qm:
                        ProcessQueryMessageForEcho(controller, qm);
                        break;
                    case BeginMatchmaking:
                    case CancelMatchmaking:
                    case GameMessage:
                    case ProposeDirectMatch:
                    case CancelDirectMatch:
                    case AcceptDirectMatch:
                        serverCore.HandleRainerMessage(controller.Tag!, message);
                        break;
                    case SupplementalDataMessageV2 sdmv2:
                        if (controller.Tag == null)
                        {
                            throw new ArgumentNullException("Received SDM, but ClientMetadata Tag for connection is null.");
                        }
                        serverCore.HandleSupplementalDataMessage(sdmv2, controller.Tag);
                        break;
                    case GetOnlineFriends gof:
                        OnlineFriendsResponse ofr;
                        if (gof.FriendIds == null || gof.FriendIds.Count == 0)
                        {
                            ofr = new OnlineFriendsResponse { CurrentlyOnlineFriends = new List<string>(0), TransactionId = gof.TransactionId };
                        }
                        else
                        {
                            List<string> matchingOnlinePlayers = gof.FriendIds.Where(serverCore.UserMetadata.ContainsKey).ToList();
                            ofr = new OnlineFriendsResponse { CurrentlyOnlineFriends = matchingOnlinePlayers, TransactionId = gof.TransactionId };
                        }
                        controller.SendMessageEnquued_EXPERIMENTAL(ofr);
                        break;
                    case GetCurrentGamesRequest:
                        ProcessConsoleGetGamesMessage(controller);
                        break;
                    case GetOnlinePlayersRequest:
                        ProcessConsoleGetOnlinePlayersMessage(controller);
                        break;
                    default:
                        break;
                }
            }
            catch(Exception e)
            {
                AnsiConsole.WriteException(e);
                messageWrapper.ReceivedFrom?.DisconnectClientImmediately();
            }
        }

        private static void ProcessConsoleGetOnlinePlayersMessage(IClientConnection controller)
        {
            GetOnlinePlayersResponse allOnlinePlayerData = new GetOnlinePlayersResponse
            { OnlinePlayers = serverCore.UserMetadata.Values.Select(um => new GetOnlinePlayersResponse.OnlinePlayerInfo { DisplayName = um.PlayerDisplayName, PlayerId = um.PlayerId, CurrentGameId = um.CurrentGame?.matchId }).ToList() };
            controller.SendMessageEnquued_EXPERIMENTAL(allOnlinePlayerData);
        }

        static void ProcessWsControlMessage(IClientConnection controller, object rawMessage)
        {
            if(rawMessage == ControlMessages.CLIENT_DISCONNECTED)
            {
                Console.WriteLine($"Player disconnected - {controller.Tag?.PlayerDisplayName ?? "[no SDM]"}");
                PlayerMetadata? playerData = controller.Tag;
                if (playerData?.PlayerId == null)
                {
                    return;
                }

                serverCore.PurgePlayerFromActivePlayers(playerData);
            }
        }

        static void ProcessConsoleGetGamesMessage(IClientConnection controller)
        {
            GetCurrentGamesResponse response = new GetCurrentGamesResponse();
            response.ongoingGames = serverCore.ActiveGamesById.Values.Select(game => new GetCurrentGamesResponse.GameSummary
            {
                GameId = game.matchId,
                Player1 = game.playerInfos[0].playerName,
                Player2 = game.playerInfos[1].playerName,
                PrizeCountPlayer1 = game.CurrentOperation?.workingBoard?.p1Prize?.Count ?? -1,
                PrizeCountPlayer2 = game.CurrentOperation?.workingBoard?.p2Prize?.Count ?? -1,
            }).ToList();

            controller.SendMessageEnquued_EXPERIMENTAL(response);
        }

        static void ProcessQueryMessageForEcho(IClientConnection controller, QueryMessage queryMsg)
        {
            if(queryMsg.queryId == REFLECT_MESSAGE_MAGIC && queryMsg.message?.Length <= 2 && 
               queryMsg.message[0] is (byte) Platform.Sdk.SerializationFormat.FlatBuffers or (byte)Platform.Sdk.SerializationFormat.JSON)
            {
                controller.SendMessageEnquued_EXPERIMENTAL(new QueryMessage { queryId = queryMsg.queryId, message = queryMsg.message }, (Platform.Sdk.SerializationFormat)queryMsg.message[0]);
            }
        }
    }
}
