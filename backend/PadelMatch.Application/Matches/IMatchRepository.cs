using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public interface IMatchRepository
{
    Task<Match?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The Match plus club/court/schedule, resolved by joining CourtSlot/Court/Club (design.md,
    /// same pattern as <c>IClubRepository.GetSlotsAsync</c>) — Match itself only stores CourtSlotId.</summary>
    Task<MatchWithSlotDetails?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddMatchAsync(Match match, CancellationToken cancellationToken);

    /// <summary>Open matches whose slot hasn't started yet (M4 discovery feed, design.md "exclusión de
    /// expirados").</summary>
    Task<IReadOnlyList<MatchWithSlotDetails>> FindOpenUpcomingAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <exception cref="CourtSlotUnavailableException">A concurrent request already booked the same CourtSlot
    /// (unique index backstop, design.md "Exclusividad del CourtSlot bajo concurrencia").</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record MatchWithSlotDetails(
    Match Match, Guid ClubId, string ClubName, double? ClubLatitude, double? ClubLongitude, string? ClubCityOrZone,
    Guid CourtId, string CourtName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
