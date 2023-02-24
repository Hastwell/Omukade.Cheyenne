using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Omukade.Cheyenne.CustomMessages
{
    [Obsolete("Use SupplementalDataMessageV2")]
    public class SupplementalDataMessage
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PlayerId { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CurrentRegion { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DeckInformationJson { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string OutfitInformationJson { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PlayerDisplayName { get; set; }

        [JsonIgnore]
        public object DeckInformationDecoded { get; set; }

        [JsonIgnore]
        public object OutfitInformationDecoded { get; set; }
    }
}
