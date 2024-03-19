using MatchLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Omukade.Cheyenne.Encoding
{
    public class WhitelistedSerializeContractResolver : SerializeResolver
    {
        static Assembly MATCH_LOGIC_ASSEMBLY = typeof(MatchLogic.AbstractBlob).Assembly;

        static internal void ReplaceContractResolvers()
        {
            SerializeResolver.instance = new WhitelistedSerializeContractResolver();
            SerializeResolver.settings.ContractResolver = SerializeResolver.instance;
            SerializeResolver.replayAuto.ContractResolver = SerializeResolver.instance;

            SerializeResolver.basic.ContractResolver = new WhitelistedContractResolver();

        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> baseProperties = base.CreateProperties(type, memberSerialization);
            
            if(type.Assembly.Equals(MATCH_LOGIC_ASSEMBLY))
            {
                HashSet<string> fieldsWithoutWhitelist = 
                    ((IEnumerable<MemberInfo>) type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                    .Concat((IEnumerable<MemberInfo>) type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                    .Where(f => f.GetCustomAttribute(typeof(JsonPropertyAttribute)) == null)
                    .Select(f => f.Name)
                    .ToHashSet();

                baseProperties = baseProperties.Where(prop => prop.PropertyName != null && !fieldsWithoutWhitelist.Contains(prop.PropertyName))
                    .ToList();
            }

            return baseProperties;
        }

        public override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty baseProperty = base.CreateProperty(member, memberSerialization);
            if(member.DeclaringType?.Assembly == MATCH_LOGIC_ASSEMBLY && member.GetCustomAttribute(typeof(JsonPropertyAttribute)) == null)
            {
                baseProperty.ShouldSerialize = (_) => false;
            }

            return baseProperty;
        }
    }
}
