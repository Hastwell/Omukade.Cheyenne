using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("Omukade.Cheyenne.Tests.Helpers.AutoParInitializer", "Omukade.Cheyenne.Tests")]

namespace Omukade.Cheyenne.Tests.Helpers
{
    public sealed class AutoParInitializer : XunitTestFramework, IDisposable
    {
        public AutoParInitializer(IMessageSink messageSink) : base(messageSink)
        {
            Omukade.Cheyenne.Patching.MatchOperationGetRandomSeedIsDeterministic.InjectRngPatchAtAll = true;
            Program.InitAutoPar();
            GameServerCore.PatchRainier();
        }
    }
}
