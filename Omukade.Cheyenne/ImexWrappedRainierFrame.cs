#if !NOIMEX
using ImexV.Core.Messages;
using ImexV.Core.Session;
using SharedSDKUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne
{
    internal class ImexWrappedRainierFrame : WargMessage, IWargMessageCustomType
    {
        public PacketFlag MessageFlag => PacketFlag.RainierWrappedBinaryFrame;

        public ServerMessage Message { get; set; }

        public override byte[] SerializePacketToBytes()
        {
            return null;
        }
    }
}
#endif