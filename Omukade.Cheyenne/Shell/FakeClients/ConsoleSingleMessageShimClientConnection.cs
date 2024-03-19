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

using Omukade.Cheyenne.ClientConnections;
using ClientNetworking;
using System.Collections.Concurrent;

namespace Omukade.Cheyenne.Shell.FakeClients
{
    public class ConsoleSingleMessageShimClientConnection<TResponseType> : IClientConnection
    {
        public TResponseType? Message;
        public ManualResetEvent WaitEvent = new ManualResetEvent(false);

        public PlayerMetadata? Tag { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsOpen => true;

        public void DisconnectClientImmediately()
        {

        }

        public void SendMessageEnquued_EXPERIMENTAL(object payload, SerializationFormat format = SerializationFormat.JSON)
        {
            if (payload is TResponseType)
            {
                Message = (TResponseType)payload;
                WaitEvent.Set();
            }
            else if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            else
            {
                throw new ArgumentException($"Console command got unexpected response type (expected {typeof(TResponseType).Name}, got {payload.GetType().Name})", nameof(payload));
            }
        }
    }
}
