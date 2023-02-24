﻿/*************************************************************************
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
