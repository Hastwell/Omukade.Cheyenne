using Omukade.Cheyenne.Tests.BakedData;
using RainierClientSDK;

namespace Omukade.Cheyenne.Tests.MultiInstanceStatic
{
    public struct ClientHandlerStaticState : IStateApplier
    {
        public string? accountId;

        public void ApplyState()
        {
            if(ClientHandler.instance == null)
            {
                InstantiateNewReferences();
            }
        }

        public void InstantiateNewReferences()
        {
            _ = new ClientHandler(new BakedRainierClient { AccountID = accountId });
        }

        public void SaveState()
        {
            accountId = ClientHandler.instance?.accountID;
        }
    }
}
