namespace Omukade.Cheyenne.CustomMessages
{
    public class GetImplementedExpandedCardsV1
    {
        /// <summary>
        /// The checksum of the client's existing data. If this matches the server's checksum, fresh data will not be returned. If not provided, or the provided checksum does not match the server's data, fresh data will be returned.
        /// </summary>
        public string? Checksum;
    }
}
