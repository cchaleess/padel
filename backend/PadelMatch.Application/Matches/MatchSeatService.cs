using PadelMatch.Application.Players;
using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public sealed class MatchSeatService(
    IMatchSeatRepository seatRepository,
    IMatchRepository matchRepository,
    IMatchAccessRepository accessRepository,
    IPlayerRepository playerRepository,
    TimeProvider clock) : IMatchSeatService
{
    public async Task<DateTimeOffset> HoldSeatAsync(
        Guid matchId, Guid playerId, int? position, CancellationToken cancellationToken)
    {
        if (position is { } requested && !MatchSeat.IsValidPosition(requested))
        {
            throw new ArgumentOutOfRangeException(nameof(position), requested, $"A seat position is 0–{MatchSeat.SeatsPerMatch - 1}.");
        }

        var now = clock.GetUtcNow();

        if (await seatRepository.HasActiveSeatAsync(matchId, playerId, now, cancellationToken))
        {
            throw new PlayerAlreadyHasSeatException();
        }

        await RequireAccessAsync(matchId, playerId, cancellationToken);

        var heldUntilUtc = now + MatchSeat.HoldDuration;
        if (!await seatRepository.TryHoldAsync(matchId, playerId, position, heldUntilUtc, now, cancellationToken))
        {
            throw new SeatUnavailableException();
        }

        return heldUntilUtc;
    }

    /// <summary>Quality rules (plan §16, m6-quality-rules): outside a competitive match's criteria, holding a seat
    /// needs an approved exception request. An unknown match is left to TryHoldAsync, which fails as unavailable.</summary>
    private async Task RequireAccessAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        if (await matchRepository.FindByIdAsync(matchId, cancellationToken) is not { } match)
        {
            return;
        }

        var player = await playerRepository.FindByIdAsync(playerId, cancellationToken);
        var shortfalls = MatchCompatibility.GetShortfalls(match, player?.Level, player?.MatchesPlayed ?? 0);
        if (shortfalls.Count == 0)
        {
            return;
        }

        var request = await accessRepository.FindRequestAsync(matchId, playerId, cancellationToken);
        if (request?.Status != AccessRequestStatus.Approved)
        {
            throw new AccessRequiresApprovalException(shortfalls);
        }
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
