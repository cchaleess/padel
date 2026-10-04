using PadelMatch.Domain.Matches;

namespace PadelMatch.Domain.Tests.Matches;

public class MatchSeatTests
{
    [Fact]
    public void CreateAvailableStartsUnclaimed()
    {
        var matchId = Guid.NewGuid();

        var seat = MatchSeat.CreateAvailable(matchId, 2);

        Assert.Equal(matchId, seat.MatchId);
        Assert.Equal(2, seat.Position);
        Assert.Equal(SeatStatus.Available, seat.Status);
        Assert.Null(seat.HolderId);
        Assert.Null(seat.HeldUntilUtc);
    }

    [Fact]
    public void CreateHeldByReservesTheSeatForFiveMinutes()
    {
        var matchId = Guid.NewGuid();
        var holderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var seat = MatchSeat.CreateHeldBy(matchId, 0, holderId, now);

        Assert.Equal(matchId, seat.MatchId);
        Assert.Equal(SeatStatus.Held, seat.Status);
        Assert.Equal(holderId, seat.HolderId);
        Assert.Equal(now.AddMinutes(5), seat.HeldUntilUtc);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void PositionsOutsideZeroToThreeAreRejected(int position)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MatchSeat.CreateAvailable(Guid.NewGuid(), position));
    }
}
