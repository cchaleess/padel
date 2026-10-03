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
        dbContext.Matches
            .Where(match => match.Id == id)
            .Join(dbContext.CourtSlots, match => match.CourtSlotId, slot => slot.Id, (match, slot) => new { match, slot })
            .Join(dbContext.Courts, x => x.slot.CourtId, court => court.Id, (x, court) => new { x.match, x.slot, court })
            .Join(dbContext.Clubs, x => x.court.ClubId, club => club.Id, (x, club) => new MatchWithSlotDetails(
                x.match, club.Id, club.Name, x.court.Id, x.court.Name, x.slot.StartsAt, x.slot.EndsAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddMatchAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);

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
