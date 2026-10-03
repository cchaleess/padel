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

    public async Task<IReadOnlyList<MatchWithSlotDetails>> FindOpenUpcomingAsync(
        DateTimeOffset now, CancellationToken cancellationToken) =>
        // The StartsAt filter is applied on the Matches-CourtSlots join directly, before the final Select builds
        // MatchWithSlotDetails: EF Core can't translate a Where() over a computed property of an already-projected
        // record (tried it, got InvalidOperationException at runtime).
        await JoinSlotDetails(dbContext.Matches.Where(match => match.Status == MatchStatus.Open), now)
            .ToListAsync(cancellationToken);

    public async Task AddMatchAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);

    private IQueryable<MatchWithSlotDetails> JoinSlotDetails(IQueryable<Match> matches, DateTimeOffset? startsAtAfter = null) =>
        matches
            .Join(dbContext.CourtSlots, match => match.CourtSlotId, slot => slot.Id, (match, slot) => new { match, slot })
            .Where(x => startsAtAfter == null || x.slot.StartsAt > startsAtAfter)
            .Join(dbContext.Courts, x => x.slot.CourtId, court => court.Id, (x, court) => new { x.match, x.slot, court })
            .Join(dbContext.Clubs, x => x.court.ClubId, club => club.Id, (x, club) => new MatchWithSlotDetails(
                x.match, club.Id, club.Name, club.Latitude, club.Longitude, club.CityOrZone,
                x.court.Id, x.court.Name, x.slot.StartsAt, x.slot.EndsAt));

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
