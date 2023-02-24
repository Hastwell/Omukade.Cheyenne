using Platform.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Omukade.Cheyenne.Tests.BakedData
{
    internal class NopClientLogger : IClientLogger
    {
        public void Error(string message)
        {
            System.Console.Error.WriteLine(message);
        }

        public void Log(string message) {}
    }
}
