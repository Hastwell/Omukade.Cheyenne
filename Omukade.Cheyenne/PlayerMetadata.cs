#if !NOIMEX
using ImexV.Core.Session;
#endif
using Omukade.Cheyenne.ClientConnections;
using Omukade.Cheyenne.Miniserver.Controllers;
using Platform.Sdk.Models.Inventory;
using SharedLogicUtils.DataTypes;
using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    public class PlayerMetadata
    {
        public Outfit? PlayerOutfit;
        public CollectionData? CurrentDeck;

        public IClientConnection PlayerConnectionHelens;
        public string? PlayerDisplayName;
        public string? PlayerId;

        public GameState? CurrentGame;

        // We don't really care what the client's current region is. However, during matchmaking,
        // the client will DC and reconnect if it needs to join a different region, which screws up IMEX..
        // Store the client's expected region so it can be parroted back as needed.
        public string? PlayerCurrentRegion;

        public long DirectMatchMakingTransactionToken;
        public string? DirectMatchMakingToken;
        public string? DirectMatchCurrentlySendingTo;
        public HashSet<string>? DirectMatchCurrentlyReceivingFrom = new HashSet<string>(1);

        public void RemovePlayerFromDirectMatchReceivingFrom(string? playerId)
        {
            if (this.DirectMatchCurrentlyReceivingFrom == null) return;
            if(playerId == null) return;

            if (this.DirectMatchCurrentlyReceivingFrom.Contains(playerId))
            {
                if (this.DirectMatchCurrentlyReceivingFrom.Count == 1) this.DirectMatchCurrentlyReceivingFrom = null;
                else this.DirectMatchCurrentlyReceivingFrom.Remove(playerId);
            }
        }
    }
}
