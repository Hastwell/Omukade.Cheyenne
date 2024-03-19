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

using Newtonsoft.Json;
using ClientNetworking;
using SharedLogicUtils.source.Logging;
using ILogger = SharedLogicUtils.source.Logging.ILogger;

namespace Omukade.Cheyenne
{
    internal class Logging
    {
        public static void WriteCharsInColor(ConsoleColor backgroundColor, ConsoleColor textColor, string text)
        {
            Console.BackgroundColor = backgroundColor;
            Console.ForegroundColor = textColor;
            Console.Write(text);
            Console.ResetColor();
        }

        public static void WriteError(string message)
        {
            WriteCharsInColor(ConsoleColor.Red, ConsoleColor.White, "!");
            Console.Write(" ");
            Console.WriteLine(message);
        }

        public static void WriteWarning(string message)
        {
            WriteCharsInColor(ConsoleColor.Yellow, ConsoleColor.Black, "!");
            Console.Write(" ");
            Console.WriteLine(message);
        }

        public static void WriteInfo(string message)
        {
            WriteCharsInColor(ConsoleColor.Blue, ConsoleColor.White, "i");
            Console.Write(" ");
            Console.WriteLine(message);
        }

        public static void WriteDebug(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("# ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void WriteSuccess(string message)
        {
            WriteCharsInColor(ConsoleColor.Green, ConsoleColor.Black, "!");
            Console.Write(" ");
            Console.WriteLine(message);
        }

        public static void WriteOperationInProgress(string message)
        {
            Console.Write("[....] ");
            Console.Write(message);
        }

        public static void WriteOperationOk()
        {
            Console.CursorLeft = 0;
            Console.Write("[ ");
            WriteCharsInColor(ConsoleColor.Black, ConsoleColor.Green, "ok");
            Console.WriteLine(" ]");
        }

        public static void WriteOperationDone()
        {
            Console.CursorLeft = 0;
            Console.Write("[");
            WriteCharsInColor(ConsoleColor.Black, ConsoleColor.Green, "done");
            Console.WriteLine("]");
        }

        public static OngoingOperationLogging WriteOperationInProgressUsing(string message)
        {
            WriteOperationInProgress(message);
            return new OngoingOperationLogging(currentLine: -1);
        }

        public static string SaveToJsonWithEnumAnnotations(object mor) => JsonConvert.SerializeObject(mor, new Newtonsoft.Json.Converters.StringEnumConverter()/*, new GuidResolvingSerializer()*/);
    }

    public class RainierLoggingAdapter : ILogger, IClientLogger
    {
        public static readonly RainierLoggingAdapter INSTANCE = new RainierLoggingAdapter();

        public void Error(string message) => Log(message, LogFlag.Error);

        public void Log(string message, LogFlag flags = LogFlag.Log)
        {
            if (flags == LogFlag.Error) Logging.WriteError(message);
#if !QUIET_RAINER_LOG
            else Logging.WriteInfo(message);
#endif
        }

        public void Log(string message) => Log(message, LogFlag.Log);

        public static void HandlePlatformError(SharedLogicUtils.DataTypes.ErrorResponse errorResponse) => Logging.WriteError(errorResponse.ToString());
    }

    public struct OngoingOperationLogging : IDisposable
    {
        public OngoingOperationLogging(int currentLine = -1)
        {
            startingLine = currentLine == -1 ? Console.CursorTop : currentLine;
        }

        int startingLine;
        public void Dispose()
        {
            int cursorTop = Console.CursorTop;
            int cursorLeft = Console.CursorLeft;

            Console.SetCursorPosition(0, startingLine);
            Logging.WriteOperationDone();
            Console.SetCursorPosition(cursorLeft, cursorTop);
        }
    }
}
