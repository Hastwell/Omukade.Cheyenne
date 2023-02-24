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

using Omukade.Cheyenne.Miniserver.Controllers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Omukade.Cheyenne
{
    public class IllegalPacketReceivedException : Exception
    {
        public IllegalPacketReceivedException() : this("Received an illegal packet from a client.") { }

        public IllegalPacketReceivedException(string message, StompController connection = null) : base(message) { this.Connection = connection; }

        public IllegalPacketReceivedException(string message, Exception innerException, StompController connection = null) : base(message, innerException) { this.Connection = connection; }

        public StompController Connection { get; private set; }
    }
}
