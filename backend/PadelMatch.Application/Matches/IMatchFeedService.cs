namespace PadelMatch.Application.Matches;

public interface IMatchFeedService
{
    Task<MatchFeed> GetFeedAsync(
        Guid playerId, double? latitude, double? longitude, string? cityOrZoneOverride, CancellationToken cancellationToken);
}

public sealed record MatchFeed(IReadOnlyList<MatchWithDistance> ForYou, IReadOnlyList<MatchWithDistance> OutOfRange);

public sealed record MatchWithDistance(MatchWithSlotDetails Details, double? DistanceKm);
