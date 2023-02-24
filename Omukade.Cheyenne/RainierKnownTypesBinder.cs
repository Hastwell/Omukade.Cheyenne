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
            Type[] TYPES_FROM_TARGETED_ASSEMBLIES = new Type[] { typeof(MatchLogic.PlayerSelectionInfo), typeof(Platform.Sdk.Models.Matchmaking.BeginMatchmaking) };
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
