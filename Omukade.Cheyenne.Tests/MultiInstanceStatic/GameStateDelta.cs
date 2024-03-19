using MatchLogic;

namespace Omukade.Cheyenne.Tests.MultiInstanceStatic
{
    public struct GameStateDelta
    {
        public GameStateDelta()
        {
            this.p1deltas = new();
            this.p2deltas = new();

            this.p1selections = new();
            this.p2selections = new();
        }

        public List<ActionModification> p1deltas;
        public List<ActionModification> p2deltas;

        public List<PlayerSelection> p1selections;
        public List<PlayerSelection> p2selections;
    }
}
