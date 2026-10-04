using Microsoft.EntityFrameworkCore;
using Npgsql;
using PadelMatch.Application.Matches;
using PadelMatch.Domain.Matches;

namespace PadelMatch.Infrastructure.Persistence;

internal sealed class MatchSeatRepository(PadelMatchDbContext dbContext) : IMatchSeatRepository
{
    private const int MaxHoldAttempts = 3;

    public async Task AddSeatAsync(MatchSeat seat, CancellationToken cancellationToken) =>
        await dbContext.MatchSeats.AddAsync(seat, cancellationToken);

    public Task<bool> HasActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.MatchSeats.AnyAsync(
            s => s.MatchId == matchId && s.HolderId == playerId &&
                 (s.Status == SeatStatus.Confirmed || (s.Status == SeatStatus.Held && s.HeldUntilUtc >= now)),
            cancellationToken);

    public async Task<bool> TryHoldAsync(
        Guid matchId, Guid playerId, int? position, DateTimeOffset heldUntilUtc, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Each attempt re-reads the candidates: a seat another request just claimed drops out of the next pick
        // (design.md, "si el UPDATE afecta 0 filas ... se reintenta una vez sobre la siguiente candidata"). A
        // requested position has a single candidate, so losing that race means the seat isn't available.
        var attempts = position is null ? MaxHoldAttempts : 1;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var candidateId = await dbContext.MatchSeats
                .Where(s => s.MatchId == matchId && (position == null || s.Position == position) &&
                            (s.Status == SeatStatus.Available || (s.Status == SeatStatus.Held && s.HeldUntilUtc < now)))
                .OrderBy(s => s.Position)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (candidateId == Guid.Empty)
            {
                return false;
            }

            int claimed;
            try
            {
                claimed = await dbContext.MatchSeats
                    .Where(s => s.Id == candidateId &&
                                (s.Status == SeatStatus.Available || (s.Status == SeatStatus.Held && s.HeldUntilUtc < now)))
                    .ExecuteUpdateAsync(setters => setters
                            .SetProperty(s => s.Status, SeatStatus.Held)
                            .SetProperty(s => s.HolderId, playerId)
                            .SetProperty(s => s.HeldUntilUtc, heldUntilUtc),
                        cancellationToken);
            }
            catch (PostgresException ex) when (IsSeatHolderUniqueViolation(ex))
            {
                // Design.md, "Backstop de concurrencia": a parallel Hold from the same player won this seat for
                // another of the match's seats first. Treated as a failed claim, same as exhausting candidates.
                return false;
            }

            if (claimed == 1)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> TryConfirmAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await dbContext.MatchSeats
            .Where(s => s.MatchId == matchId && s.HolderId == playerId &&
                        s.Status == SeatStatus.Held && s.HeldUntilUtc >= now)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.Status, SeatStatus.Confirmed)
                    .SetProperty(s => s.ConfirmedAtUtc, now),
                cancellationToken) == 1;

    public async Task<bool> TryLeaveAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken) =>
        await dbContext.MatchSeats
            .Where(s => s.MatchId == matchId && s.HolderId == playerId && s.Status == SeatStatus.Confirmed &&
                        dbContext.Matches.Any(m => m.Id == matchId && m.Status == MatchStatus.Open))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.Status, SeatStatus.Available)
                    .SetProperty(s => s.HolderId, (Guid?)null)
                    .SetProperty(s => s.HeldUntilUtc, (DateTimeOffset?)null)
                    .SetProperty(s => s.ConfirmedAtUtc, (DateTimeOffset?)null),
                cancellationToken) == 1;

    public Task<Guid?> FindEarliestConfirmedAsync(Guid matchId, CancellationToken cancellationToken) =>
        // Seats confirmed before ConfirmedAtUtc existed have it null: they count as the oldest, by position.
        dbContext.MatchSeats
            .Where(s => s.MatchId == matchId && s.Status == SeatStatus.Confirmed)
            .OrderBy(s => s.ConfirmedAtUtc.HasValue)
            .ThenBy(s => s.ConfirmedAtUtc)
            .ThenBy(s => s.Position)
            .Select(s => s.HolderId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryReleaseAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken) =>
        await dbContext.MatchSeats
            .Where(s => s.MatchId == matchId && s.HolderId == playerId && s.Status == SeatStatus.Held)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.Status, SeatStatus.Available)
                    .SetProperty(s => s.HolderId, (Guid?)null)
                    .SetProperty(s => s.HeldUntilUtc, (DateTimeOffset?)null),
                cancellationToken) == 1;

    public Task<int> CountConfirmedAsync(Guid matchId, CancellationToken cancellationToken) =>
        dbContext.MatchSeats.CountAsync(s => s.MatchId == matchId && s.Status == SeatStatus.Confirmed, cancellationToken);

    public Task<PlayerSeat?> FindActiveSeatAsync(Guid matchId, Guid playerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.MatchSeats
            .Where(s => s.MatchId == matchId && s.HolderId == playerId &&
                        (s.Status == SeatStatus.Confirmed || (s.Status == SeatStatus.Held && s.HeldUntilUtc >= now)))
            .Select(s => new PlayerSeat(s.Position, s.Status, s.HeldUntilUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ConfirmedPlayer>> GetConfirmedPlayersAsync(Guid matchId, CancellationToken cancellationToken) =>
        await dbContext.MatchSeats
            .Where(s => s.MatchId == matchId && s.Status == SeatStatus.Confirmed)
            // Ordered on an anonymous shape, then projected: EF can't translate an OrderBy over an already-projected
            // record (same limitation as MatchRepository's feed query).
            .Join(dbContext.Players, s => s.HolderId, p => (Guid?)p.Id, (s, p) => new { s.Position, p.Id, p.DisplayName, p.Level })
            .OrderBy(x => x.Position)
            .Select(x => new ConfirmedPlayer(x.Position, x.Id, x.DisplayName, x.Level))
            .ToListAsync(cancellationToken);

    private static bool IsSeatHolderUniqueViolation(PostgresException ex) =>
        ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "IX_MatchSeats_MatchId_HolderId";
}
