namespace PadelMatch.Application.Matches;

public sealed class MatchSeatService(
    IMatchSeatRepository seatRepository, IMatchRepository matchRepository, TimeProvider clock) : IMatchSeatService
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(5);

    public async Task<DateTimeOffset> HoldSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        if (await seatRepository.HasActiveSeatAsync(matchId, playerId, now, cancellationToken))
        {
            throw new PlayerAlreadyHasSeatException();
        }

        var heldUntilUtc = now + HoldDuration;
        if (!await seatRepository.TryHoldAsync(matchId, playerId, heldUntilUtc, now, cancellationToken))
        {
            throw new SeatUnavailableException();
        }

        return heldUntilUtc;
    }

    public async Task ConfirmSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        if (!await seatRepository.TryConfirmAsync(matchId, playerId, now, cancellationToken))
        {
            throw new SeatNotHeldException();
        }

        if (await seatRepository.CountConfirmedAsync(matchId, cancellationToken) == 4)
        {
            await matchRepository.MarkFullAsync(matchId, cancellationToken);
        }
    }

    public async Task ReleaseSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        if (!await seatRepository.TryReleaseAsync(matchId, playerId, cancellationToken))
        {
            throw new SeatNotHeldException();
        }
    }
}
