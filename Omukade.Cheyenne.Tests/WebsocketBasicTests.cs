using FlatBuffers;
using Microsoft.AspNetCore.Builder;
using MonoMod.Utils;
using Omukade.Cheyenne.Miniserver.Controllers;
using Omukade.Cheyenne.Tests.BakedData;
using Platform.Sdk;
using Platform.Sdk.Codecs;
using Platform.Sdk.Flatbuffers;
using Platform.Sdk.Models.Query;
using Platform.Sdk.Models.WebSocket;
using Platform.Sdk.Stomp;
using Platform.Sdk.Util;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using SerializationFormat = Platform.Sdk.SerializationFormat;

namespace Omukade.Cheyenne.Tests
{
    public class WebsocketBasicTests : IDisposable
    {
        WebApplication app;

        static HttpRouter router = new HttpRouter(new BakedData.BakedStage
        {
            BypassValidation = true,
            GlobalRoute = new BakedData.BakedRoute
            {
                WebsocketUrl = new Uri("ws://localhost:10850/websocket/v1/external/stomp")
            },
            name = "LOCAL",
            SupportsRouting = false
        });

        static WebSocketSettings wsSettings = new WebSocketSettings
        {
            ConnectAttemptTimeout = new TimeSpan(0, 0, 5),
            HeartbeatTimeout = new TimeSpan(0, 0, 30),
            InactivityHeartbeat = new TimeSpan(0, 0, 30),
            ReconnectTimeout = new TimeSpan(0, 0, 30),
            StifleHeartbeat = true
        };

        static TokenHolder fakeTokenHolder = new TokenHolder(null);

        public WebsocketBasicTests()
        {
            //RegisterDecoders();
            RegisterEncoders();


            // Initialize the BufferPool or the FlatBuffer serializer will do dumb shit like get stuck in a while(true) loop
            // while working on a zero-length buffer.
            BufferPool.Init(new BufferPoolSettings());

            Omukade.Cheyenne.Program.InitAutoPar();
            app = Omukade.Cheyenne.Program.PrepareWsServer();
            app.StartAsync().Wait();
            Omukade.Cheyenne.Program.StartWsProcessorThread();
        }

        private static void RegisterEncoders()
        {
            Dictionary<Type, Action<object, FlatBufferBuilder>> encoderMap = (Dictionary<Type, Action<object, FlatBufferBuilder>>)typeof(Encoders).GetField("Map", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)!
                .GetValue(null)!;

            encoderMap[typeof(QueryMessage)] = new Action<object, FlatBufferBuilder>(EncodeQueryMessage);
        }

        static object DecodeHeartbeatMessage(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_websocket.HeartbeatPayload rootAsHeartbeatPayload = com.pokemon.studio.contracts.client_websocket.HeartbeatPayload.GetRootAsHeartbeatPayload(bb);
            return new Platform.Sdk.Models.WebSocket.HeartbeatPayload() { timeSent = rootAsHeartbeatPayload.TimeSent };
        }
        static void EncodeQueryMessage(object obj, FlatBufferBuilder builder)
        {
            QueryMessage qm = (QueryMessage)obj;

            StringOffset queryIdOffset = builder.CreateStringOrNull(qm.queryId);
            VectorOffset dataOffset = builder.CreateByteArray(qm.message);

            com.pokemon.studio.contracts.client_gameserver.QueryMessage.StartQueryMessage(builder);
            com.pokemon.studio.contracts.client_gameserver.QueryMessage.AddQueryId(builder, queryIdOffset);
            com.pokemon.studio.contracts.client_gameserver.QueryMessage.AddMessage(builder, dataOffset);
            builder.Finish(com.pokemon.studio.contracts.client_gameserver.QueryMessage.EndQueryMessage(builder).Value);
        }

        public void Dispose()
        {
            Omukade.Cheyenne.Program.continueRunningWsServer = false;
            app.StopAsync().Wait();
            app.DisposeAsync().AsTask().Wait();
        }

        [Theory]
        [InlineData(SerializationFormat.JSON)]
        [InlineData(SerializationFormat.FlatBuffers)]
        public void SingleClientCanCommunicate(SerializationFormat FORMAT_TO_USE)
        {
            const string QUERY_ID_TO_USE = Omukade.Cheyenne.Program.REFLECT_MESSAGE_MAGIC;
            byte[] PAYLOAD_TO_USE = new byte[] { (byte)FORMAT_TO_USE }; //System.Text.Encoding.UTF8.GetBytes(QUERY_ID_TO_USE);

            ManualResetEvent serverToClientMessageEvent = new ManualResetEvent(false);

            ICodec CODEC_TO_USE = CodecUtil.Codec(FORMAT_TO_USE);

            bool receivedServerToClientMessage = false;

            WebsocketWrapper.MessageDispatcher md = (ref StompFrame frame, ReusableBuffer buffer) =>
            {
                QueryMessage payload = CODEC_TO_USE.Deserialize<QueryMessage>(buffer);
                if (payload.queryId == QUERY_ID_TO_USE && PAYLOAD_TO_USE.SequenceEqual(payload.message))
                {
                    serverToClientMessageEvent.Set();
                    receivedServerToClientMessage = true;
                }
            };

            WebsocketWrapper wsw = new WebsocketWrapper(logger: new NopClientLogger(), router, token: fakeTokenHolder, dispatcher: md, CODEC_TO_USE, settings: wsSettings, enableHeartbeats: false, enableMessageReceipts: false, null);
            try
            {
                wsw.OpenAsync().Wait();
                wsw.SendCommand(new Command<QueryMessage>("/app/foo", false), new QueryMessage { queryId = QUERY_ID_TO_USE, message = PAYLOAD_TO_USE });

                serverToClientMessageEvent.WaitOne(2_000 /*ms*/);
            }
            finally
            {
                wsw.CloseAsync().Wait();
            }

            Assert.True(receivedServerToClientMessage, "Server to client message was not received");
        }
       
        [Fact]
        public void SingleClientCanCommunicateMultiplePackets()
        {
            const int NUM_COMMANDS_TO_SEND = 10;
            const string QUERY_ID_TO_USE = Omukade.Cheyenne.Program.REFLECT_MESSAGE_MAGIC;

            ManualResetEvent allMessagesReceived = new ManualResetEvent(false);

            ICodec CODEC_TO_USE = CodecUtil.Codec(SerializationFormat.JSON);

            Queue<byte> messagesYetToBeReceived = new Queue<byte>();
            for (byte i = 0; i < NUM_COMMANDS_TO_SEND; i++) messagesYetToBeReceived.Enqueue((byte) i);

            WebsocketWrapper.MessageDispatcher md = (ref StompFrame frame, ReusableBuffer buffer) =>
            {
                QueryMessage payload = CODEC_TO_USE.Deserialize<QueryMessage>(buffer);
                if (payload.queryId == QUERY_ID_TO_USE && messagesYetToBeReceived.Peek() == payload.message[1])
                {
                    messagesYetToBeReceived.Dequeue();
                }
                else
                {
                    throw new InvalidOperationException($"Received out-of-order message - got {payload.message[1]}, but was expecting {messagesYetToBeReceived.Peek()}");
                }

                if (messagesYetToBeReceived.Count == 0) allMessagesReceived.Set();
            };

            WebsocketWrapper wsw = new WebsocketWrapper(logger: new NopClientLogger(), router, token: fakeTokenHolder, dispatcher: md, CODEC_TO_USE, settings: wsSettings, enableHeartbeats: false, enableMessageReceipts: false, null);
            bool receivedSignal = false;
            try
            {
                wsw.OpenAsync().Wait();
                for(int i = 0; i < NUM_COMMANDS_TO_SEND; i++)
                {
                    wsw.SendCommand(new Command<QueryMessage>("/app/foo", false), new QueryMessage { queryId = QUERY_ID_TO_USE, message = new byte[] { (byte) SerializationFormat.JSON, (byte) i } });
                }

                receivedSignal = allMessagesReceived.WaitOne(2_000 /*ms*/);
            }
            finally
            {
                wsw.CloseAsync().Wait();
            }

            Assert.True(receivedSignal, $"Not all messages received, or received out-of-order; {messagesYetToBeReceived.Count} messages remain: {string.Join(", ", messagesYetToBeReceived)}");
        }
    }
}