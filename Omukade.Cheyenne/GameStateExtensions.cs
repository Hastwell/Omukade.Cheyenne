using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    static internal class GameStateExtensions
    {
        public static string GetPlayerEntityId(this GameState state, string playerId)
        {
            ArgumentNullException.ThrowIfNull(state, nameof(state));
            ArgumentNullException.ThrowIfNull(playerId, nameof(playerId));

            if (playerId == state.CurrentOperation.workingBoard.player1.ownerPlayerId)
            {
                return state.CurrentOperation.workingBoard.player1.entityID;
            }
            else if (playerId == state.CurrentOperation.workingBoard.player2.ownerPlayerId)
            {
                return state.CurrentOperation.workingBoard.player2.entityID;
            }
            throw new ArgumentOutOfRangeException(nameof(playerId), "Specified player is neither Player 1 nor Player 2!");
        }
    }
}
