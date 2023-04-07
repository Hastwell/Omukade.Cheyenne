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

//#define BAKE_RNG

using FlatBuffers;
using HarmonyLib;
using ICSharpCode.SharpZipLib.GZip;
using MatchLogic;
using Newtonsoft.Json;
using Omukade.Cheyenne.Encoding;
using Platform.Sdk.Models;
using RainierClientSDK;
using RainierClientSDK.source.OfflineMatch;
using SharedLogicUtils.source.Services.Query.Responses;
using SharedSDKUtils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

    // Since SV is always in effect, optimize this call from "check ruleset" to "always true".
    // See you in 2026 when this screws up rule evaluation when the rules change again!
    [HarmonyPatch(typeof(MatchBoard))]
    [HarmonyPatch(nameof(MatchBoard.IsRuleSet2023), MethodType.Getter)]
    static class FeatureSetPerformanceBoost
    {
        /* RELEASE
.method private hidebysig static bool  Implementation(class [MatchLogic]MatchLogic.MatchBoard board) cil managed
{
  .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = ( 01 00 01 00 00 ) 
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   instance class [System.Collections]System.Collections.Generic.Dictionary`2<string,bool> [MatchLogic]MatchLogic.MatchBoard::get_featureSet()
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000d
  IL_0009:  pop
  IL_000a:  ldc.i4.0
  IL_000b:  br.s       IL_0019
  IL_000d:  ldstr      "RuleChanges2023"
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       instance bool class [System.Collections]System.Collections.Generic.Dictionary`2<string,bool>::TryGetValue(!0,
                                                                                                                                 !1&)
  IL_0019:  brfalse.s  IL_001d
  IL_001b:  ldloc.0
  IL_001c:  ret
  IL_001d:  ldc.i4.0
  IL_001e:  ret
} // end of method FeatureSetPerformanceBoost::Implementation
         */
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> originalInstructions)
        {
            // Instructions for "return true;"
            List<CodeInstruction> instructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Ret)
            };

            return instructions;
        }

        static bool Implementation(MatchBoard board)
        {
            if(board.featureSet?.TryGetValue("RuleChanges2023", out bool isSvRulesEnabled) == true)
            {
                return isSvRulesEnabled;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(OfflineAdapter), nameof(OfflineAdapter.ReceiveOperation))]
    public static class ReceiveOperationShowsIlOffsetsInErrors
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo EXCEPTION_GET_STACKTRACE = AccessTools.PropertyGetter(typeof(Exception), nameof(Exception.StackTrace));
            MethodInfo ENCHANCED_STACKTRACE = AccessTools.Method(typeof(ReceiveOperationShowsIlOffsetsInErrors), nameof(ReceiveOperationShowsIlOffsetsInErrors.GetStackTraceWithIlOffsets));

            foreach (CodeInstruction instruction in instructions)
            {
                if(instruction.Calls(EXCEPTION_GET_STACKTRACE))
                {
                    // replace stacktrace call with out enchanced stackframe method that returns IL offsets
                    yield return new CodeInstruction(OpCodes.Call, ENCHANCED_STACKTRACE);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        public static string GetStackTraceWithIlOffsets(Exception ex)
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(ex);
            StringBuilder sb = new StringBuilder();
            PrepareStacktraceString(sb, st);
            string preparedStacktrace = sb.ToString();

            Program.ReportError(ex);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }

            return preparedStacktrace;
        }

        private static void PrepareStacktraceString(StringBuilder sb, StackTrace st)
        {
            foreach (System.Diagnostics.StackFrame frame in st.GetFrames())
            {
                try
                {
                    MethodBase? frameMethod = frame.GetMethod();
                    if (frameMethod == null)
                    {
                        sb.Append("[null method] - at IL_");
                    }
                    else
                    {
                        string frameClass = frameMethod.DeclaringType?.FullName ?? "(null)";
                        string frameMethodDisplayName = frameMethod.Name;
                        int frameMetadataToken = frameMethod.MetadataToken;
                        sb.Append($"{frameClass}::{frameMethodDisplayName} @{frameMetadataToken:X8} - at IL_");
                    }
                    sb.Append(frame.GetILOffset().ToString("X4"));
                    sb.AppendLine();
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[error on this frame - {ex.GetType().FullName} - {ex.Message}]");
                }
            }
        }
    }

    [HarmonyPatch]
    static class UseWhitelistedResolverMatchOperation
    {
        static readonly WhitelistedContractResolver resolver = new WhitelistedContractResolver();

        static IEnumerable<MethodBase> TargetMethods() => typeof(MatchOperation)
            .GetConstructors();

        [HarmonyPostfix]
        static void Postfix(MatchOperation __instance)
        {
            __instance.settings.ContractResolver = resolver;
        }
    }

    [HarmonyPatch]
    static class UseWhitelistedResolverMatchBoard
    {
        static readonly WhitelistedContractResolver resolver = new WhitelistedContractResolver();

        static IEnumerable<MethodBase> TargetMethods() => typeof(MatchBoard)
            .GetConstructors();

        [HarmonyPostfix]
        static void Postfix(MatchBoard __instance)
        {
            __instance.settings.ContractResolver = resolver;
        }
    }

    [HarmonyPatch]
    static class UseWhitelistedResolverGameState
    {
        static readonly WhitelistedContractResolver resolver = new WhitelistedContractResolver();

        static IEnumerable<MethodBase> TargetMethods() => typeof(GameState)
            .GetConstructors();

        [HarmonyPostfix]
        static void Postfix(GameState __instance)
        {
            __instance.settings.ContractResolver = resolver;
        }
    }
}
