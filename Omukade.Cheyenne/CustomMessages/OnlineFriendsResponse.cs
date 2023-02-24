using System;
using System.Collections.Generic;
using System.Text;

namespace Omukade.Cheyenne.CustomMessages
{
    public class OnlineFriendsResponse
    {
        /// <summary>
        /// All requested players that are currently online.
        /// </summary>
        public List<string> CurrentlyOnlineFriends { get; set; }
        public uint TransactionId { get; set; }
    }
}
