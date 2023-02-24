#if !NOIMEX
using ImexV.Core.Messages.InternalMessages.RainierShim;
#endif

using ICSharpCode.SharpZipLib.GZip;
using MatchLogic;
using Newtonsoft.Json;
using Platform.Sdk.Models.GameServer;
using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    internal static class MessageExtensions
    {
        public static PlayerMessage AsPlayerMessage(this ServerMessage smg)
        {
            return new PlayerMessage() { gameId = smg.matchID, message = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(smg)) };
        }

        public static void SetPrecompressedBody(this ServerMessage smg, byte[] precompressedPayload)
        {
            smg.compressedValue = precompressedPayload;
        }

        public static byte[] PrecompressObject(object obj, int compressionLevel = 9)
        {
            string jsonValue = JsonConvert.SerializeObject(obj, SerializeResolver.settings);
            using MemoryStream cachedDataBytes = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonValue));
            using MemoryStream outputData = new MemoryStream(25_000);

            GZip.Compress(cachedDataBytes, outputData, true, bufferSize: 4096, level: compressionLevel);
            return outputData.ToArray();
        }

#if !NOIMEX
        public static SerializedPacket ToSerializedPacket(this SupplementalDataMessage sdm) => SerializedPacket.FromSerializedPacket(sdm);
#endif
    }
}
