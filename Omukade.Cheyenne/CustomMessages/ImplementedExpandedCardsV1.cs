using Newtonsoft.Json;

namespace Omukade.Cheyenne.CustomMessages
{
    public class ImplementedExpandedCardsV1
    {
        /// <summary>
        /// A list of of valid card IDs, seperated by pipe symbols ("|"). If this value's checksum matched the one provided in <see cref="GetImplementedExpandedCardsV1.Checksum"/>, this value will be null.
        /// </summary>
        public string? RawImplementedCardNames;

        [JsonIgnore]
        public IEnumerable<string>? ImplementedCardNames
        {
            get
            {
                return RawImplementedCardNames?.Split('|');
            }
            set
            {
                if (value == null) { this.RawImplementedCardNames = null; return; }

                this.RawImplementedCardNames = string.Join('|', value.OrderBy(v => v));

                IEnumerable<string> hashBytes = System.Security.Cryptography.SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(this.RawImplementedCardNames))
                    .Select(b => b.ToString("02x"));

                this.Checksum = string.Concat(hashBytes);
            }
        }

        /// <summary>
        /// Checksum of valid card IDs. Always returned.
        /// </summary>
        public string Checksum;
    }
}
