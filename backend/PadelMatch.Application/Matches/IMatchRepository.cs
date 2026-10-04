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
    /// expirados") and that have at least one Confirmed seat, excluding those <paramref name="viewerId"/> organized
    /// or already has an active seat in — the feed only lists matches the viewer can join (m5-mobile-confirmation).</summary>
    Task<IReadOnlyList<MatchWithSlotDetails>> FindJoinableUpcomingAsync(
        Guid viewerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Matches (Open or Full) whose slot hasn't started yet where the player has a Confirmed seat,
    /// soonest first — the feed's "confirmed" and "pending confirmation" sections (m5-mobile-confirmation).</summary>
    Task<IReadOnlyList<MatchWithSlotDetails>> FindUpcomingConfirmedForPlayerAsync(
        Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Flips Open to Full when the 4th seat confirms (design.md, "Open→Full"). A no-op, not an error,
    /// if the match isn't Open anymore — the ExecuteUpdateAsync WHERE clause makes it idempotent.</summary>
    Task MarkFullAsync(Guid matchId, CancellationToken cancellationToken);

    /// <summary>Hands the organizer role over (plan §18), only if <paramref name="fromId"/> still holds it.</summary>
    Task TransferOrganizerAsync(Guid matchId, Guid fromId, Guid toId, CancellationToken cancellationToken);

    /// <exception cref="CourtSlotUnavailableException">A concurrent request already booked the same CourtSlot
    /// (unique index backstop, design.md "Exclusividad del CourtSlot bajo concurrencia").</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record MatchWithSlotDetails(
    Match Match, Guid ClubId, string ClubName, double? ClubLatitude, double? ClubLongitude, string? ClubCityOrZone,
    Guid CourtId, string CourtName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int ConfirmedSeats);
