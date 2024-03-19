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

using ClientNetworking;

namespace Omukade.Cheyenne.ClientConnections
{
    public interface IClientConnection
    {
        public void SendMessageEnquued_EXPERIMENTAL(object payload, SerializationFormat format = SerializationFormat.JSON);
        public void DisconnectClientImmediately();
        public PlayerMetadata? Tag { get; set; }
        public bool IsOpen { get; }
    }
}
