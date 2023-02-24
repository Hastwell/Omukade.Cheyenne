using Newtonsoft.Json;
using SharedLogicUtils.DataTypes;

namespace Omukade.Cheyenne.CustomMessages
{
    public class SupplementalDataMessageV2
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PlayerId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CurrentRegion { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CollectionData? DeckInformation { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Outfit? OutfitInformation { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PlayerDisplayName { get; set; }
    }
}
