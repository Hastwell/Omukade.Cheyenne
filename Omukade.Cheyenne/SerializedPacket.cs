#if !NOIMEX
using ImexV.Core.Messages.InternalMessages.RainierShim;
#endif
using Newtonsoft.Json;
using SharedLogicUtils.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    [Obsolete("Use PlayerMetadata")]
    internal class SerializedPacket
    {
        //public string PayloadDataType;
        public object JsonPayload;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CollectionData ADDL_DeckList;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ADDL_UserDisplayName;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ADDL_CurrentRegion;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Outfit AADL_Outfit;

        public string UserId;

        [JsonIgnore]
        public bool Precompressed = false;
#if !NOIMEX
        public static SerializedPacket FromSerializedPacket(SupplementalDataMessage sdm) =>
            new SerializedPacket
            {
                UserId = sdm.PlayerId,
                AADL_Outfit = (Outfit) sdm.OutfitInformationDecoded,
                ADDL_CurrentRegion = sdm.CurrentRegion,
                ADDL_DeckList = (CollectionData) sdm.DeckInformationDecoded,
                ADDL_UserDisplayName = sdm.PlayerDisplayName,
            };
#endif
    }
}
