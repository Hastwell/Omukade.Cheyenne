using System;
using System.Collections.Generic;
using System.Text;

namespace Omukade.Cheyenne.CustomMessages
{
    public class GetOnlineFriends
    {
        /// <summary>
        /// All player IDs this player considers to be a friend.
        /// </summary>
        public List<string> FriendIds { get; set; }
        public uint TransactionId { get; set; }
    }
}
