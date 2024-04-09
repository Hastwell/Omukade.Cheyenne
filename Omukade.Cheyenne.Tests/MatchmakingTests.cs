using Omukade.Cheyenne.Matchmaking;

namespace Omukade.Cheyenne.Tests
{
    public class BasicMatchmakingTests
    {
        [Fact]
        public void CanBePairedForMatch()
        {
            PlayerMetadata player1 = new() { PlayerId = Guid.NewGuid().ToString() };
            PlayerMetadata player2 = new() { PlayerId = Guid.NewGuid().ToString() };
            bool matchWasMade = true;

            IMatchmakingSwimlane swimlane = new BasicMatchmakingSwimlane(MatchLogic.GameplayType.Friend, MatchLogic.GameMode.Standard, (callbackSwimlane,a,b) =>
            {
                Assert.NotNull(callbackSwimlane);
                Assert.Equal(player1, a);
                Assert.Equal(player2, b);
                matchWasMade = true;
            });

            swimlane.EnqueuePlayer(player1);
            swimlane.EnqueuePlayer(player2);

            Assert.True(matchWasMade);
        }
    }
}
