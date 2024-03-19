using MatchLogic;
using System.Diagnostics.CodeAnalysis;

namespace Omukade.Cheyenne.Tests.Helpers
{
    public class EntityIdComparator : IEqualityComparer<CardEntity>
    {
        public bool Equals(CardEntity? x, CardEntity? y) => x?.entityID == y?.entityID;

        public int GetHashCode([DisallowNull] CardEntity obj) => obj.entityID.GetHashCode();
    }
}
