using Microsoft.EntityFrameworkCore;
using Npgsql;
using PadelMatch.Application.Matches;
using PadelMatch.Domain.Matches;

namespace PadelMatch.Infrastructure.Persistence;

internal sealed class MatchRepository(PadelMatchDbContext dbContext) : IMatchRepository
{
    public Task<Match?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Matches.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<MatchWithSlotDetails?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken) =>
        JoinSlotDetails(dbContext.Matches.Where(match => match.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MatchWithSlotDetails>> FindJoinableUpcomingAsync(
        Guid viewerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        // The StartsAt filter is applied on the Matches-CourtSlots join directly, before the final Select builds
        // MatchWithSlotDetails: EF Core can't translate a Where() over a computed property of an already-projected
        // record (tried it, got InvalidOperationException at runtime).
        // Only matches with at least one Confirmed seat (until the organizer pays, the match isn't a real
        // opportunity yet), and none the viewer organized or already holds a seat in: the feed answers "what can
        // I join?" (plan §13, m5-mobile-confirmation design.md).
        await JoinSlotDetails(
                dbContext.Matches.Where(match => match.Status == MatchStatus.Open &&
                    match.OrganizerId != viewerId &&
                    dbContext.MatchSeats.Any(s => s.MatchId == match.Id && s.Status == SeatStatus.Confirmed) &&
                    !dbContext.MatchSeats.Any(s => s.MatchId == match.Id && s.HolderId == viewerId &&
                        (s.Status == SeatStatus.Confirmed || (s.Status == SeatStatus.Held && s.HeldUntilUtc >= now)))),
                now)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MatchWithSlotDetails>> FindUpcomingConfirmedForPlayerAsync(
        Guid playerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        (await JoinSlotDetails(
                dbContext.Matches.Where(match => dbContext.MatchSeats.Any(s =>
                    s.MatchId == match.Id && s.HolderId == playerId && s.Status == SeatStatus.Confirmed)),
                now)
            .ToListAsync(cancellationToken))
        // Ordered in memory: same EF limitation as above, the projected record's StartsAt can't be ordered on in SQL.
        .OrderBy(m => m.StartsAt)
        .ToList();

    public async Task AddMatchAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);

    public async Task MarkFullAsync(Guid matchId, CancellationToken cancellationToken) =>
        await dbContext.Matches
            .Where(m => m.Id == matchId && m.Status == MatchStatus.Open)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.Status, MatchStatus.Full), cancellationToken);

    private IQueryable<MatchWithSlotDetails> JoinSlotDetails(IQueryable<Match> matches, DateTimeOffset? startsAtAfter = null) =>
        matches
            .Join(dbContext.CourtSlots, match => match.CourtSlotId, slot => slot.Id, (match, slot) => new { match, slot })
            .Where(x => startsAtAfter == null || x.slot.StartsAt > startsAtAfter)
            .Join(dbContext.Courts, x => x.slot.CourtId, court => court.Id, (x, court) => new { x.match, x.slot, court })
            .Join(dbContext.Clubs, x => x.court.ClubId, club => club.Id, (x, club) => new MatchWithSlotDetails(
                x.match, club.Id, club.Name, club.Latitude, club.Longitude, club.CityOrZone,
                x.court.Id, x.court.Name, x.slot.StartsAt, x.slot.EndsAt,
                // Correlated subquery inside the same SELECT (m5-mobile-confirmation design.md): only Confirmed
                // seats count, Held ones aren't shown to other players (plan §11).
                dbContext.MatchSeats.Count(s => s.MatchId == x.match.Id && s.Status == SeatStatus.Confirmed)));

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsCourtSlotUniqueViolation(ex))
        {
            throw new CourtSlotUnavailableException();
        }
    }

    private static bool IsCourtSlotUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Matches_CourtSlotId"
        };
}
