//#define ENABLE_CHECKPOINTS
using Mono.Cecil;
using Omukade.Cheyenne.Shell.FakeClients;
using Omukade.Cheyenne.Shell.Model;
using SharedSDKUtils;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Omukade.Cheyenne
{
    partial class Program
    {
        static Style SPECTRE_STYLE_ERROR = new Style(foreground: Color.Red);
        static Style SPECTRE_STYLE_WARNING = new Style(foreground: Color.Yellow);
        static Style SPECTRE_STYLE_SUCCESS = new Style(foreground: Color.Lime);

        static Style SPECTRE_STYLE_P1 = new Style(foreground: Color.Cyan1);
        static Style SPECTRE_STYLE_P2 = new Style(foreground: Color.Magenta1);

        static void CmdShell()
        {
            bool continueShell = true;
            while (continueShell)
            {
                Console.Write("OMU> ");
                string? cmdRaw = Console.ReadLine();
                if (cmdRaw == null) break;
                if (string.IsNullOrWhiteSpace(cmdRaw)) continue;

                string[] cmdSplit = cmdRaw.Split(' ');
                string cmdName = cmdSplit[0];
                switch(cmdName)
                {
                    case "games":
                        GetCurrentGames();
                        break;
                    case "players":
                        GetConnectedPlayers();
                        break;
                    default:
                        AnsiConsole.WriteLine($"Unknown command: {cmdName}");
                        break;
                }
            }
        }

        static TResponse SendWsMessageAndWaitForResponse<TResponse>(object request)
        {
            ConsoleSingleMessageShimClientConnection<TResponse> fakeClient = new ConsoleSingleMessageShimClientConnection<TResponse>();
            receiveQueue.Enqueue(new Miniserver.Model.ReceivedMessage { ReceivedFrom = fakeClient, Message = request });

            fakeClient.WaitEvent.WaitOne();
            return fakeClient.Message;
        }

        static void GetCurrentGames()
        {
            GetCurrentGamesResponse ongoingGames = SendWsMessageAndWaitForResponse<GetCurrentGamesResponse>(new GetCurrentGamesRequest());

            Console.WriteLine($"Number of games: {ongoingGames.ongoingGames.Count}");
            var ongoingGamesTable = new Table();
            ongoingGamesTable.AddColumn("Game ID");
            ongoingGamesTable.AddColumn("Player 1");
            ongoingGamesTable.AddColumn("Player 2");
            ongoingGamesTable.AddColumn("Prizes", (tc) => tc.Centered());

            foreach (GetCurrentGamesResponse.GameSummary game in ongoingGames.ongoingGames)
            {
                ongoingGamesTable.AddRow(game.GameId, game.Player1, game.Player2, $"{game.PrizeCountPlayer1} - {game.PrizeCountPlayer2}");
            }

            AnsiConsole.Write(ongoingGamesTable);
        }

        static void GetConnectedPlayers()
        {
            GetOnlinePlayersResponse onlinePlayers = SendWsMessageAndWaitForResponse<GetOnlinePlayersResponse>(new GetOnlinePlayersRequest());

            Console.WriteLine($"Online Players: {onlinePlayers.OnlinePlayers.Count}");
            var onlinePlayersTable = new Table();
            onlinePlayersTable.AddColumns("Player ID", "IGN", "Current Game");

            foreach(GetOnlinePlayersResponse.OnlinePlayerInfo player in onlinePlayers.OnlinePlayers)
            {
                onlinePlayersTable.AddRow(player.PlayerId ?? "[not sent]", player.DisplayName ?? "[not sent]", player.CurrentGameId ?? "[not in a game]");
            }

            AnsiConsole.Write(onlinePlayersTable);
        }
    }
}
