using System.Text.Json.Serialization;

namespace Omukade.Cheyenne.Model
{       
    public class PlayerCountModel
    {
        [JsonPropertyName("count")]
        public int PlayerCount;

        [JsonPropertyName("players"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? PlayerNames;
    }
}
