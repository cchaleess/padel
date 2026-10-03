using PadelMatch.Domain.Matches;

namespace PadelMatch.Domain.Tests.Matches;

public class MatchSeatTests
{
    [Fact]
    public void CreateAvailableStartsUnclaimed()
    {
        var matchId = Guid.NewGuid();

        var seat = MatchSeat.CreateAvailable(matchId);

        Assert.Equal(matchId, seat.MatchId);
        Assert.Equal(SeatStatus.Available, seat.Status);
        Assert.Null(seat.HolderId);
        Assert.Null(seat.HeldUntilUtc);
    }
}
