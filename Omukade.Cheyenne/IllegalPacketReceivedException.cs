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
