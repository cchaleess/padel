namespace PadelMatch.Application.Matches;

public interface IMatchFeedService
{
    Task<MatchFeed> GetFeedAsync(
        Guid playerId, double? latitude, double? longitude, string? cityOrZoneOverride, CancellationToken cancellationToken);
}

/// <param name="Confirmed">Upcoming Full matches where the player has a confirmed seat: the match is closed.</param>
/// <param name="PendingConfirmation">Upcoming Open matches where the player has a confirmed seat, still waiting for
/// the rest to fill (plan §13: a match leaves the public feed when full but stays visible to its participants).</param>
public sealed record MatchFeed(
    IReadOnlyList<MatchWithDistance> Confirmed,
    IReadOnlyList<MatchWithDistance> PendingConfirmation,
    IReadOnlyList<MatchWithDistance> ForYou,
    IReadOnlyList<MatchWithDistance> OutOfRange);

public sealed record MatchWithDistance(MatchWithSlotDetails Details, double? DistanceKm);
