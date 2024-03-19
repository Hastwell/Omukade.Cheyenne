namespace Omukade.Cheyenne.Tests.MultiInstanceStatic
{
    public interface IStateApplier
    {
        void ApplyState();
        void SaveState();
        void InstantiateNewReferences();
    }
}
