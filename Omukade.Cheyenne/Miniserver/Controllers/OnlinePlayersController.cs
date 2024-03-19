using Microsoft.AspNetCore.Mvc;
using Omukade.Cheyenne.ClientConnections;
using Omukade.Cheyenne.Model;
using Omukade.Cheyenne.Shell.FakeClients;
using Omukade.Cheyenne.Shell.Model;

namespace Omukade.Cheyenne.Miniserver.Controllers
{

    [Route("api/v1")]
    [ApiController]
    public class OnlinePlayersController : ControllerBase
    {
        static DateTime playerCountCachedTime = default;
        static int cachedPlayerCount = -1;

        static DateTime playerListCachedTime = default;
        static List<string> cachedPlayers = new List<string>() { "CACHE_ENTRY_INVALID" };

        static readonly TimeSpan cacheTimeout = TimeSpan.FromSeconds(3);

        [HttpGet]
        [Route("players")]
        public async Task<IActionResult> GetOnlinePlayers([FromQuery(Name = "names")] bool playerNames = false)
        {
            if(!playerNames && DateTime.UtcNow - OnlinePlayersController.playerCountCachedTime < OnlinePlayersController.cacheTimeout)
            {
                return Ok(new PlayerCountModel { PlayerCount = cachedPlayerCount});
            }
            else if(playerNames && DateTime.UtcNow - OnlinePlayersController.playerListCachedTime < OnlinePlayersController.cacheTimeout)
            {
                return Ok(new PlayerCountModel { PlayerCount = cachedPlayerCount, PlayerNames = cachedPlayers });
            }


            if(StompController.MessageReceived == null)
            {
                return StatusCode(500, "StompController MessageReceived not initialized.");
            }

            ConsoleSingleMessageShimClientConnection<GetOnlinePlayersResponse> clientConnection = new();

            GetOnlinePlayersResponse internalGetPlayerDataResponse = await Task.Run(() => 
            {
                StompController.MessageReceived!.Invoke(clientConnection, new GetOnlinePlayersRequest() { PlayerCountOnly = !playerNames });
                clientConnection.WaitEvent.WaitOne(TimeSpan.FromSeconds(5));

                if(clientConnection.Message == null)
                {
                    throw new Exception("Got a playercount response, but is somehow null");
                }

                return clientConnection.Message;
            }).ConfigureAwait(false);

            PlayerCountModel returnModel = new PlayerCountModel { PlayerCount = internalGetPlayerDataResponse.PlayerCount };

            OnlinePlayersController.cachedPlayerCount = returnModel.PlayerCount;
            OnlinePlayersController.playerCountCachedTime = DateTime.UtcNow;

            if(playerNames && internalGetPlayerDataResponse.OnlinePlayers != null)
            {
                returnModel.PlayerNames = new List<string>(internalGetPlayerDataResponse.OnlinePlayers.Count);
                foreach(GetOnlinePlayersResponse.OnlinePlayerInfo playerData in internalGetPlayerDataResponse.OnlinePlayers)
                {
                    returnModel.PlayerNames.Add(playerData.DisplayName!);
                }

                OnlinePlayersController.cachedPlayers = returnModel.PlayerNames;
                OnlinePlayersController.playerListCachedTime = DateTime.UtcNow;
            }

            return Ok(returnModel);                    
        }
    }
}