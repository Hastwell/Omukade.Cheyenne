using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    public class ConfigSettings
    {
        [JsonProperty(PropertyName = "carddata-directory")]
        public string CardDataDirectory = "PTCGL-CardDefinitions";

        [JsonProperty(PropertyName = "ws-port")]
        public int HttpPort = 10850;

        [JsonProperty(PropertyName = "ws-secure-port")]
        public int HttpSecurePort = 10851;

        [JsonProperty(PropertyName = "cardsource-overrides-enable")]
        public bool CardSourceOverridesEnable;

        [JsonProperty(PropertyName = "cardsource-overrides-directory")]
        public string CardSourceOverridesDirectory;

        [JsonProperty(PropertyName = "disable-player-order-randomization")]
        public bool DisablePlayerOrderRandomization;

        [JsonProperty(PropertyName = "discord-error-webhook-enable")]
        public bool EnableDiscordErrorWebhook;

        [JsonProperty(PropertyName = "discord-error-webhook-url")]
        public string? DiscordErrorWebhookUrl;

        [JsonProperty(PropertyName = "autopar-search-folder")]
        public string AutoParSearchFolder = "autopar";
    }
}
