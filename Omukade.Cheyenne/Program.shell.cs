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

using Mono.Cecil;
using Omukade.Cheyenne.ClientConnections;
using Omukade.Cheyenne.CustomMessages;
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
            Console.CancelKeyPress += Console_CancelKeyPress;

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
                    case "dgs":
                    case "dumpgamestate":
                        DumpGameState(cmdSplit[1]);
                        break;
                    case "exit":
                    case "quit":
                    case "stop":
                        TerminateConsole();
                        return;
                    default:
                        AnsiConsole.WriteLine($"Unknown command: {cmdName}");
                        break;
                }
            }
        }

        static void DumpGameState(string gameId)
        {
            SendWsMessage(new DumpGameStateRequest(gameId));
        }

        static void TerminateConsole()
        {
            bool stoppedInTime = StopWsServer().AsTask().Wait(10_000);
            if (stoppedInTime) return;

            Console.WriteLine("Server did not stop with 10s; force-killing.");
            Environment.Exit(2);
        }

        static void SendWsMessage(object request)
        {
            DebugClientConnection debugClient = new DebugClientConnection();
            receiveQueue.Enqueue(new Miniserver.Model.ReceivedMessage { ReceivedFrom = debugClient, Message = request });
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
                onlinePlayersTable.AddRow(player.PlayerId ?? "[not sent]", player.DisplayName ?? "(not sent)", player.CurrentGameId ?? "(not in a game)");
            }

            AnsiConsole.Write(onlinePlayersTable);
        }
        private static void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("CTRL-C received; preparing to stop");
            TerminateConsole();
            Environment.Exit(0); // The app doesn't seem to want to exit on its own when CTRL-C'd, so just shank it in the back.
        }
    }
}
