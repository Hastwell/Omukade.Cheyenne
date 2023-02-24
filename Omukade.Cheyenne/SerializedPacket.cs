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
