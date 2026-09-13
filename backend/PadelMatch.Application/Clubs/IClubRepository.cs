using PadelMatch.Domain.Clubs;

namespace PadelMatch.Application.Clubs;

public interface IClubRepository
{
    /// <summary>All clubs. No pagination in M2: the expected catalog size (development seed plus early
    /// user submissions) doesn't justify it yet — see design.md "Alternativas y límites".</summary>
    Task<IReadOnlyList<Club>> GetAllAsync(CancellationToken cancellationToken);

    Task<Club?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddClubAsync(Club club, CancellationToken cancellationToken);

    Task<IReadOnlyList<Court>> GetCourtsByClubIdAsync(Guid clubId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CourtSlotWithCourtName>> GetSlotsAsync(
        Guid clubId, Guid? courtId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record CourtSlotWithCourtName(CourtSlot Slot, string CourtName);
