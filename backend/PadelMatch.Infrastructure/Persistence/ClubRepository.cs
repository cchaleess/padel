using Microsoft.EntityFrameworkCore;
using PadelMatch.Application.Clubs;
using PadelMatch.Domain.Clubs;

namespace PadelMatch.Infrastructure.Persistence;

internal sealed class ClubRepository(PadelMatchDbContext dbContext) : IClubRepository
{
    public async Task<IReadOnlyList<Club>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Clubs.ToListAsync(cancellationToken);

    public Task<Club?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Clubs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddClubAsync(Club club, CancellationToken cancellationToken) =>
        await dbContext.Clubs.AddAsync(club, cancellationToken);

    public async Task<IReadOnlyList<Court>> GetCourtsByClubIdAsync(Guid clubId, CancellationToken cancellationToken) =>
        await dbContext.Courts.Where(c => c.ClubId == clubId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CourtSlotWithCourtName>> GetSlotsAsync(
        Guid clubId, Guid? courtId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var query = dbContext.CourtSlots
            .Join(dbContext.Courts, slot => slot.CourtId, court => court.Id, (slot, court) => new { slot, court })
            .Where(x => x.court.ClubId == clubId
                        && x.slot.Status == SlotStatus.Available
                        && x.slot.StartsAt >= from
                        && x.slot.StartsAt <= to);

        if (courtId is { } id)
        {
            query = query.Where(x => x.slot.CourtId == id);
        }

        var results = await query.OrderBy(x => x.slot.StartsAt).ToListAsync(cancellationToken);
        return results.Select(x => new CourtSlotWithCourtName(x.slot, x.court.Name)).ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
