using RainierClientSDK;

namespace Omukade.Cheyenne.Tests.MultiInstanceStatic
{
    public struct CardInfoStaticState : IStateApplier
    {
        public Dictionary<string, CardInfo> cardInfos;
        public Dictionary<string, CardInfo> savedCardInfos;

        public void ApplyState()
        {
            CardInfo.cardInfos = this.cardInfos;
            CardInfo.savedCardInfos = this.savedCardInfos;
        }

        public void InstantiateNewReferences()
        {
            CardInfo.cardInfos = new();
            CardInfo.savedCardInfos = null;
        }

        public void SaveState()
        {
            this.cardInfos = CardInfo.cardInfos;
            this.savedCardInfos = CardInfo.savedCardInfos;
        }
    }
}
