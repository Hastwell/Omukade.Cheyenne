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

using ICSharpCode.SharpZipLib.GZip;
using MatchLogic;
using Newtonsoft.Json;
using Omukade.Cheyenne.Encoding;
using ClientNetworking.Models.GameServer;
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
            return new PlayerMessage() { gameId = smg.matchID, message = FasterJson.FastSerializeToBytes(smg) };
        }

        public static GameMessage AsGameMessage(this ServerMessage smg)
        {
            return new GameMessage() { gameId = smg.matchID, message = FasterJson.FastSerializeToBytes(smg) };
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

        public static T GetValueEfficently<T>(this ServerMessage smg)
        {
            if(smg.compressedValue == null)
            {
                return default;
            }

            using MemoryStream ms = new MemoryStream(smg.compressedValue);
            using GZipInputStream decompressedStream = new GZipInputStream(ms);
            using StreamReader textReader = new StreamReader(decompressedStream);
            using JsonTextReader jsonReader = new JsonTextReader(textReader);

            JsonSerializer serializer = JsonSerializer.Create(DeserializeResolver.settings);
            return serializer.Deserialize<T>(jsonReader);
        }
    }
}
