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

using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    public class RainierKnownTypesBinder : ISerializationBinder
    {
        public static readonly RainierKnownTypesBinder INSTANCE;

        static Dictionary<string, Type> RECOGNIZED_TYPES;
        static RainierKnownTypesBinder()
        {
            Type[] TYPES_FROM_TARGETED_ASSEMBLIES = new Type[] { typeof(MatchLogic.PlayerSelectionInfo), typeof(ClientNetworking.Models.Matchmaking.BeginMatchmaking) };
            RECOGNIZED_TYPES = TYPES_FROM_TARGETED_ASSEMBLIES.Select(t => t.Assembly)
                .SelectMany(t => t.GetTypes())
                .Where(t => t.FullName != "MatchLogic.AbstractBlob") // Prevent gadget attacks
                .ToDictionary(TypeToLookupName);

            INSTANCE = new RainierKnownTypesBinder();
        }

        private static string TypeToLookupName(Type t) => $"{t.Assembly.GetName().Name}+{t.FullName}";

        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            string lookupName = TypeToLookupName(serializedType);
            if(!RECOGNIZED_TYPES.ContainsKey(lookupName))
            {
                throw new ArgumentException($"Serialization of non-whitelisted type is not supported: {serializedType.FullName}");
            }

            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }

        public Type BindToType(string? assemblyName, string typeName)
        {

            if(RECOGNIZED_TYPES.TryGetValue($"{assemblyName}+{typeName}", out Type resolvedType))
            {
                return resolvedType;
            }

            throw new ArgumentException($"Deserialization of non-whitelisted type is not supported: {assemblyName}+{typeName}");
        }
    }
}
