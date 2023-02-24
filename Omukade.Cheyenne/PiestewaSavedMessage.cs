using Newtonsoft.Json;
using System.Text;

namespace Omukade.Piestewa.Core
{
    public class LoggedMessage
    {
        public LoggedMessage() { }

        public LoggedMessage(StompCommand stompCommand, string stompDestination, object stompPayload, string? receiptId = null) :
            this(stompCommand, stompDestination, MessageFormat.JSON, stompPayload.GetType().Name, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(stompPayload)))
        {

        }
        public LoggedMessage(StompCommand stompCommand, string stompDestination, MessageFormat format, string stompPayloadDatatype, byte[] stompPayload, string? receiptId = null)
        {
            if (stompCommand == StompCommand.Unknown || stompCommand == StompCommand.NotApplicable)
            {
                throw new ArgumentException($"{nameof(stompCommand)} cannot be {nameof(StompCommand.Unknown)} or {nameof(StompCommand.NotApplicable)}");
            }
            if (stompPayload == null) throw new ArgumentNullException(nameof(stompPayload));

            this.StompVerb = stompCommand;
            this.StompDestination = stompDestination;
            this.Protocol = MessageProtocol.WebsocketStomp;
            this.MessageSeen = DateTime.UtcNow;
            this.Format = format;
            this.MessageTypeRaw = stompPayloadDatatype;

            string formatMime = format == MessageFormat.JSON ? "application/json" : "application/octet-stream";
            StringBuilder sb = new StringBuilder($"{stompCommand}\ndestination:{stompDestination}\npay:{stompPayloadDatatype}\ncontent-type:{formatMime}\ncontent-length:{stompPayload.Length}\n\n");
            string stompHeaderString = sb.ToString();
            int stompHeaderByteCount = Encoding.UTF8.GetByteCount(stompHeaderString);
            int bufferSize = stompHeaderByteCount + stompPayload.Length;

            this.MessagePayload = new byte[bufferSize];
            Encoding.UTF8.GetBytes(stompHeaderString, this.MessagePayload);

            stompPayload.CopyTo(this.MessagePayload, stompHeaderByteCount);
        }

        public LoggedMessage(MessageDirection direction, MessageProtocol protocol, MessageFormat format, StompCommand stompVerb, string? stompDestination, string messageTypeRaw, byte[] rawPayload, string? stompReceipt = null, int? stompReceiptId = null)
        {
            this.Direction = direction;
            this.Protocol = protocol;
            this.Format = format;
            this.StompVerb = stompVerb;
            this.StompDestination = stompDestination;
            this.MessageTypeRaw = messageTypeRaw;
            this.StompReceipt = stompReceipt;
            this.StompReceiptId = stompReceiptId;
            this.MessageSeen = DateTime.UtcNow;
            this.MessagePayload = rawPayload;
        }

        public enum MessageDirection : byte
        {
            Unknown, ClientToServer, ServerToClient
        }

        public enum MessageProtocol : byte
        {
            Unknown, HTTP, WebsocketStomp
        }

        public enum MessageFormat : byte
        {
            Unknown, JSON, Flatbuffer
        }

        public enum StompCommand : byte
        {
            Unknown,
            /// <summary>
            /// Only valid with <see cref="MessageProtocol.HTTP"/>, which does not have the concept of Stomp verbs. It is invalid to use this with <see cref="MessageProtocol.WebsocketStomp"/>.
            /// </summary>
            NotApplicable,
            SEND,
            SUBSCRIBE,
            UNSUBSCRIBE,
            BEGIN,
            COMMIT,
            ABORT,
            ACK,
            NACK,
            DISCONNECT,
            CONNECT,
            STOMP,
            CONNECTED,
            MESSAGE,
            RECEIPT,
            ERROR,
        }

        // v1.0 Fields
        public int MessageId;
        public DateTime MessageSeen;
        public MessageDirection Direction;
        public MessageProtocol Protocol { get; set; }
        public MessageFormat Format { get; set; }
        public StompCommand StompVerb { get; set; } = StompCommand.NotApplicable;
        public string? StompDestination { get; set; }
        public string MessageTypeRaw { get; set; }
        public string? StompReceipt { get; set; }
        public int? StompReceiptId { get; set; }

        /// <summary>
        /// The raw payload of the message. For <see cref="MessageProtocol.WebsocketStomp"/>, includes the STOMP header.
        /// </summary>
        public byte[]? MessagePayload { get; set; }
    }
}