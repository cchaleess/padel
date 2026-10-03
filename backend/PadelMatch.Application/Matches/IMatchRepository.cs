using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public interface IMatchRepository
{
    Task<Match?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The Match plus club/court/schedule, resolved by joining CourtSlot/Court/Club (design.md,
    /// same pattern as <c>IClubRepository.GetSlotsAsync</c>) — Match itself only stores CourtSlotId.</summary>
    Task<MatchWithSlotDetails?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddMatchAsync(Match match, CancellationToken cancellationToken);

    /// <exception cref="CourtSlotUnavailableException">A concurrent request already booked the same CourtSlot
    /// (unique index backstop, design.md "Exclusividad del CourtSlot bajo concurrencia").</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record MatchWithSlotDetails(
    Match Match, Guid ClubId, string ClubName, Guid CourtId, string CourtName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
