using ClientNetworking;
using RainierClientSDK;
using RainierClientSDK.Inventory.Models;
using RainierClientSDK.source.Matchmaking;
using RainierClientSDK.source.TimeProviderImplementation;
using SharedLogicUtils.Config;
using SharedLogicUtils.DataTypes;

namespace Omukade.Cheyenne.Tests.BakedData
{
    public struct BakedRainierClient : IRainierClient
    {
        public IClient Client => throw new NotImplementedException();

        public MockPlatformSDK.MockPlatformClient MockClient => throw new NotImplementedException();

        public OfflineClient OfflineClient => throw new NotImplementedException();

        public IConfigLoader ConfigLoader => throw new NotImplementedException();

        public ClientTimeProvider ClientTimeProvider => throw new NotImplementedException();

        public string AccountID { get; set; }

        public string InstallID => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public bool UseMock => throw new NotImplementedException();

        public bool UseOffline => throw new NotImplementedException();

        public ClientHandler.ClientStatus Status => throw new NotImplementedException();

        public void ClearOnPlayerMessage()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public Task RefreshAccessToken(string accessToken, Action<ErrorResponse> onError = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Setup(bool useMock, string installID, string PTCSJWT, Action<bool, string> loginCallback, Action<NetworkStatus> networkStatusChange, Action<string> rainierSDKLogCallback, ClientMessageHandler clientMessageHandler, ClientSetupContext clientSetupContext, Stage stage, string region, string localStorageDirectory, InventoryConfig inventoryConfig, string idfv, string locale, int ppID, string appSku, string deviceID, string deviceType, string deviceOS, string clientSessionID, MatchMakerTimerSettings matchMakerTimerSettings, SharedLogicUtils.source.Logging.ILogger iLogger, IClientLogger platformLogger = null, Action<ErrorResponse> onError = null)
        {
            throw new NotImplementedException();
        }

        public void StartOfflineMessageHandler(OfflineClient offlineClient, Dictionary<int, bool> featureFlags)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SwitchOfflineWithFeatures(bool useOffline, MatchMakerTimerSettings matchMakerTimerSettings, Dictionary<int, bool> features)
        {
            throw new NotImplementedException();
        }
    }
}
