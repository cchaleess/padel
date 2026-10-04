using PadelMatch.Application.Players;
using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public sealed class MatchSeatService(
    IMatchSeatRepository seatRepository,
    IMatchRepository matchRepository,
    IMatchAccessRepository accessRepository,
    IMatchAccessService accessService,
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
            // No seat left to grant: pending exception requests expire now, so their players are told instead of
            // waiting on a vote that can't help them (m6-mobile-quality-rules).
            await accessRepository.ExpirePendingRequestsAsync(matchId, clock.GetUtcNow(), cancellationToken);
        }
    }

    public async Task LeaveSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        var details = await matchRepository.FindDetailsByIdAsync(matchId, cancellationToken)
            ?? throw new MatchNotFoundException(matchId);
        if (details.StartsAt <= clock.GetUtcNow())
        {
            throw new MatchAlreadyStartedException();
        }

        // Once all four seats are paid the match is closed and its seats can't be left from the app; cancellation
        // policies for that case aren't decided yet (m7-leave-match).
        if (details.Match.Status == MatchStatus.Full)
        {
            throw new MatchClosedException();
        }

        if (!await seatRepository.TryLeaveAsync(matchId, playerId, cancellationToken))
        {
            // Either no Confirmed seat, or the match closed between the check above and the UPDATE.
            throw (await matchRepository.FindByIdAsync(matchId, cancellationToken))?.Status == MatchStatus.Full
                ? new MatchClosedException()
                : new NoConfirmedSeatException();
        }

        if (details.Match.OrganizerId == playerId &&
            await seatRepository.FindEarliestConfirmedAsync(matchId, cancellationToken) is { } successor)
        {
            await matchRepository.TransferOrganizerAsync(matchId, playerId, successor, cancellationToken);
        }

        await accessService.ReevaluatePendingRequestsAsync(matchId, cancellationToken);
    }

    public async Task ReleaseSeatAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        if (!await seatRepository.TryReleaseAsync(matchId, playerId, cancellationToken))
        {
            throw new SeatNotHeldException();
        }
    }
}
