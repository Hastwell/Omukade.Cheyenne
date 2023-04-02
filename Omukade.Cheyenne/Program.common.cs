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
using Omukade.AutoPAR;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("Omukade.Cheyenne.Tests")]

namespace Omukade.Cheyenne
{
    internal partial class Program
    {
        static internal ConfigSettings config;
        static GameServerCore serverCore;

        static internal void Main(string[] args)
        {
            Console.WriteLine("Omukade Cheyenne");
            Console.WriteLine("(c) 2022 Electrosheep Networks");

            InitAutoPar();
            Init();

            WebApplication app = PrepareWsServer();
            StartWsProcessorThread();
            app.Start();
            CmdShell();
        }

        static internal void InitAutoPar()
        {
            if (File.Exists("config.json"))
            {
                config = JsonConvert.DeserializeObject<ConfigSettings>(File.ReadAllText("config.json"))!;
            }
            else
            {
                Console.WriteLine("Config file not found; loading defaults");
                config = new ConfigSettings();
            }

            Console.WriteLine("Injecting AutoPAR...");
            string? searchFolder = config.AutoParSearchFolder ?? AutoPAR.InstallationFinder.FindPtcglInstallAssemblyDirectory();

            if (searchFolder == null)
            {
                Console.Error.WriteLine("AutoPAR: PTCGL folder not configured, and PTCGL could not be autodetected.");
                Environment.Exit(1);
                return;
            }

            AssemblyLoadInterceptor.Initialize(searchFolder);
        }

        static private void Init()
        {
            serverCore = new GameServerCore(config);

            Console.WriteLine("Patching Rainier...");
            GameServerCore.PatchRainier();

            Console.WriteLine("Preloading/Precompressing Heavy Data...");
            GameServerCore.RefreshSharedGameRules(config);            
        }

        /// <summary>
        /// Converts a string to either a GUID (if the string could be parsed as a GUID), or a GUID derived from the MD5 hash of the string. This method is deterministic.
        /// </summary>
        /// <param name="input">A string that either is a string GUID, or an arbitrary string.</param>
        /// <returns>A GUID.</returns>
        private static Guid StringToGuidOrHash(string input)
        {
            if (Guid.TryParse(input, out Guid parsedGuid)) return parsedGuid;

            int byteLen = System.Text.Encoding.UTF8.GetByteCount(input);
            Span<byte> inputBytes = byteLen < 1024 ? stackalloc byte[byteLen] : new byte[byteLen];
            System.Text.Encoding.UTF8.GetBytes(input, inputBytes);

            Span<byte> hashBytes = stackalloc byte[128 / 8];
            MD5.HashData(inputBytes, hashBytes);

            Guid rtrn = new Guid(hashBytes);
            return rtrn;
        }

        internal static void ReportError(Exception ex)
        {
            StringBuilder consoleErrorStringBuilder = new StringBuilder();
            consoleErrorStringBuilder.Append($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ssZ} Exception : " + ex.GetType().Name);
            WriteInnerExceptionToStringbuider(ex, consoleErrorStringBuilder);
            consoleErrorStringBuilder.AppendLine(ex.StackTrace);

            if (config.EnableDiscordErrorWebhook && config.DiscordErrorWebhookUrl != null)
            {
                StringBuilder messageToSend = new StringBuilder();
                messageToSend.Append("Exception on ");
                messageToSend.Append(System.Net.Dns.GetHostName());
                messageToSend.AppendLine(" : " + ex.GetType().Name);
                if (ex.Message != null) messageToSend.AppendLine(ex.Message);
                WriteInnerExceptionToStringbuider(ex, messageToSend);
                if (ex.StackTrace != null)
                {
                    messageToSend.Append("```");
                    messageToSend.Append(ex.StackTrace);
                    messageToSend.Append("```");
                }


                SendDiscordAlert(messageToSend.ToString(), config.DiscordErrorWebhookUrl);
            }
        }

        static void WriteInnerExceptionToStringbuider(Exception ex, StringBuilder sb)
        {
            if (ex is AggregateException aex && aex.InnerExceptions != null)
            {
                foreach (var innerEx in aex.InnerExceptions)
                {
                    sb.Append("Inner: ");
                    sb.Append(innerEx.GetType().FullName);
                    if (innerEx.Message != null)
                    {
                        sb.Append(" - ");
                        sb.AppendLine(innerEx.Message);
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
            }
        }

        static void SendDiscordAlert(string message, string? webhookEndpoint)
        {
            if (!config.EnableDiscordErrorWebhook || webhookEndpoint == null) return;
            string payload = JsonConvert.SerializeObject(new { content = message });
            StringContent content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            HttpClient myClient = new HttpClient();

            HttpResponseMessage httpResponse = myClient.PostAsync(webhookEndpoint, content).Result;
        }
    }
}
