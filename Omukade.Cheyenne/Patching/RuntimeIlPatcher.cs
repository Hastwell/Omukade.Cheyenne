//#define BAKE_RNG

using FlatBuffers;
using HarmonyLib;
using ICSharpCode.SharpZipLib.GZip;
using MatchLogic;
using Newtonsoft.Json;
using Platform.Sdk.Models;
using RainierClientSDK;
using RainierClientSDK.source.OfflineMatch;
using SharedLogicUtils.source.Services.Query.Responses;
using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static MatchLogic.RainierServiceLogger;
using LogLevel = MatchLogic.RainierServiceLogger.LogLevel;

namespace Omukade.Cheyenne.Patching
{
    [HarmonyPatch(typeof(MatchOperation), "GetRandomSeed")]
    public static class MatchOperationGetRandomSeedIsDeterministic
    {
        public const int RngSeed = 654654564;

        /// <summary>
        /// Controls if this patch is applied. This has no effect once Harmony has finished patching. Once patched, RNG baking can be controlled using <see cref="ShouldBakeRng"/>.
        /// Do not enable this patch unless needed (eg, testing), as there is minor performance impact.
        /// </summary>
        public static bool ShouldPatchRng = false;

        /// <summary>
        /// If <see cref="ShouldPatchRng"/> is set, RNG calls for games will be controlled using this patch instead of using the built-in RNG. The built-in RNG can be enabled/disabled at any time by toggling this field.
        /// </summary>
        public static bool ShouldBakeRng = true;

        public static Random Rng = new Random(RngSeed);

        public static void ResetRng() => Rng = new Random(RngSeed);

        static bool Prepare(MethodBase original) => ShouldBakeRng;

        static bool Prefix(ref int __result)
        {
            if(!ShouldBakeRng)
            {
                return true;
            }

            __result = Rng.Next();
            return false;
        }
    }

    [HarmonyPatch(typeof(RainierServiceLogger), nameof(RainierServiceLogger.Log))]
    static class RainierServiceLoggerLogEverything
    {
        public static bool BE_QUIET = true;

        static bool Prefix(string logValue, LogLevel logLevel)
        {
            if (BE_QUIET) return false;

            Logging.WriteDebug(logValue);
            return false;
        }
    }

    [HarmonyPatch(typeof(OfflineAdapter))]
    static class OfflineAdapterHax
    {
        internal static GameServerCore parentInstance;

        [HarmonyPrefix]
        [HarmonyPatch("LogMsg")]
        static bool QuietLogMsg() => false;

        [HarmonyPrefix]
        [HarmonyPatch("ResolveOperation")]
        static bool ResolveOperationViaCheyenne(ref bool __result, string accountID, MatchOperation currentOperation, GameState state, string messageID)
        {
            __result = parentInstance.ResolveOperation(state, currentOperation, isInputUpdate: false);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(OfflineAdapter.SendMessage), typeof(ServerMessage))]
        static bool SendMessageViaCheyenne(ServerMessage message)
        {
            try
            {
                GameServerCore.SendPacketToClient(parentInstance.UserMetadata.GetValueOrDefault(message.accountID), message.AsPlayerMessage());
            }
            catch(Exception e)
            {
                Console.WriteLine($"SendMessage Error :: {e.GetType().FullName} - {e.Message}");
                throw;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DeckInfo))]
    static class DeckInfoHax
    {
        [HarmonyReversePatch]
        [HarmonyPatch("ImportMetadata")]
        public static DeckInfo ImportMetadata(ref DeckInfo deck, string metadata, Action<ErrorResponse> onError)
        {
            throw new NotImplementedException("This should be patched by Harmony. If you are reading this, there is a fundamental problem with this code.");
        }
    }
}
