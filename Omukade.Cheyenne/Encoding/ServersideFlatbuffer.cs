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

using com.pokemon.studio.contracts.client_gameserver;
using com.pokemon.studio.contracts.client_user;
using com.pokemon.studio.contracts.client_websocket;
using FlatBuffers;
using Platform.Sdk.Models;
using Platform.Sdk.Models.GameServer;
using Platform.Sdk.Models.Matchmaking;
using Platform.Sdk.Models.Query;
using Platform.Sdk.Models.User;
using Platform.Sdk.Models.WebSocket;

namespace Omukade.Cheyenne.Encoding
{
    public class ServersideFlatbufferEncoders
    {
        public static Platform.Sdk.Models.Matchmaking.BeginMatchmaking DecodeBeginMatchmaking(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_matchmaker.BeginMatchmaking modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_matchmaker.BeginMatchmaking>(bb);
            return new(modelMessage.Txid, modelMessage.GetContextBytes()?.ToArray());
        }

        public static Platform.Sdk.Models.Matchmaking.CancelMatchmaking DecodeCancelMatchmaking(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_matchmaker.CancelMatchmaking modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_matchmaker.CancelMatchmaking>(bb);
            return new(modelMessage.Txid);
        }

        public static Platform.Sdk.Models.GameServer.GameMessage DecodeGameMessage(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_gameserver.GameMessage modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_gameserver.GameMessage>(bb);
            return new Platform.Sdk.Models.GameServer.GameMessage { gameId = modelMessage.GameId, message = modelMessage.GetMessageBytes()?.ToArray() };
        }
        public static Platform.Sdk.Models.Matchmaking.ProposeDirectMatch DecodeProposeDirectMatch(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_matchmaker.ProposeDirectMatch modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_matchmaker.ProposeDirectMatch>(bb);
            SignedAccountId? sai = DecodeSignedAccountIdModel(modelMessage.TargetAccountId);
            return new(modelMessage.Txid, sai, modelMessage.GetContextBytes()?.ToArray());
        }

        public static Platform.Sdk.Models.Matchmaking.CancelDirectMatch DecodeCancelDirectMatch(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_matchmaker.CancelDirectMatch modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_matchmaker.CancelDirectMatch>(bb);
            return new Platform.Sdk.Models.Matchmaking.CancelDirectMatch(DecodeSignedAccountIdModel(modelMessage.TargetAccountId), DecodeSignedMatchContextModel(modelMessage.SignedMatchContext));
        }

        public static Platform.Sdk.Models.Matchmaking.AcceptDirectMatch DecodeAcceptDirectMatch(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_matchmaker.AcceptDirectMatch modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_matchmaker.AcceptDirectMatch>(bb);
            return new Platform.Sdk.Models.Matchmaking.AcceptDirectMatch(modelMessage.Txid, DecodeDirectMatchInvitationModel(modelMessage.Invitation), modelMessage.GetContextBytes()?.ToArray());
        }

        internal static Platform.Sdk.Models.Query.QueryMessage DecodeQueryMessage(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_gameserver.QueryMessage modelMessage = DecodeCommon<com.pokemon.studio.contracts.client_gameserver.QueryMessage>(bb);
            return new Platform.Sdk.Models.Query.QueryMessage { queryId = modelMessage.QueryId, message = modelMessage.GetMessageBytes()?.ToArray() };
        }

        public static DataStoreSaveRequest DecodeDataStoreSaveRequest(ByteBuffer bb)
        {
            UserDataSaveRequest udsrModel = DecodeCommon<UserDataSaveRequest>(bb);
            DataStoreSaveRequest dssr = new DataStoreSaveRequest(udsrModel.Key, udsrModel.GetDataBytes()?.ToArray(), udsrModel.ExpectedVersion);
            return dssr;
        }

        internal static Platform.Sdk.Models.WebSocket.HeartbeatPayload DecodeHeartbeatPayload(ByteBuffer bb)
        {
            com.pokemon.studio.contracts.client_websocket.HeartbeatPayload heartbeatPayload = DecodeCommon<com.pokemon.studio.contracts.client_websocket.HeartbeatPayload>(bb);
            return new Platform.Sdk.Models.WebSocket.HeartbeatPayload() { timeSent = heartbeatPayload.TimeSent };
        }

        static TModel DecodeCommon<TModel>(ByteBuffer bb) where TModel : struct, IFlatbufferObject
        {
            TModel modelMessage = default;
            modelMessage.__init(bb.GetInt(bb.Position) + bb.Position, bb);
            return modelMessage;
        }

        static SignedAccountId? DecodeSignedAccountIdModel(com.pokemon.studio.contracts.client_signeddata.SignedAccountId? saiModel)
        {
            SignedAccountId? sai = saiModel == null ? null : new SignedAccountId(saiModel.Value.AccountId, saiModel.Value.IssuedAt, saiModel.Value.GetSignatureBytes()?.ToArray());
            return sai;
        }

        static SignedMatchContext? DecodeSignedMatchContextModel(com.pokemon.studio.contracts.client_signeddata.SignedMatchContext? smcModel)
        {
            SignedMatchContext? smc = smcModel == null ? null : new SignedMatchContext(smcModel.Value.MmToken, smcModel.Value.IssuedAt, smcModel.Value.GetMatchContextBytes()?.ToArray(), smcModel.Value.GetSignatureBytes()?.ToArray());
            return smc;
        }

        static DirectMatchInvitation? DecodeDirectMatchInvitationModel(com.pokemon.studio.contracts.client_matchmaker.DirectMatchInvitation? dmiModel)
        {
            DirectMatchInvitation? dmi = dmiModel == null ? null : new DirectMatchInvitation(dmiModel.Value.SourceAccountId, dmiModel.Value.MmToken, dmiModel.Value.IssuedAt, dmiModel.Value.GetSharedContextBytes()?.ToArray(), dmiModel.Value.GetSignatureBytes()?.ToArray());
            return dmi;
        }
    }
}
