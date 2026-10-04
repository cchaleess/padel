using PadelMatch.Application.Players;
using PadelMatch.Domain.Clubs;
using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public sealed class MatchFeedService(
    IMatchRepository matchRepository, IPlayerRepository playerRepository, TimeProvider clock) : IMatchFeedService
{
    public async Task<MatchFeed> GetFeedAsync(
        Guid playerId, double? latitude, double? longitude, string? cityOrZoneOverride, CancellationToken cancellationToken)
    {
        var matches = await matchRepository.FindJoinableUpcomingAsync(playerId, clock.GetUtcNow(), cancellationToken);
        var player = await playerRepository.FindByIdAsync(playerId, cancellationToken);

        var forYou = new List<MatchWithSlotDetails>();
        var outOfRange = new List<MatchWithSlotDetails>();
        foreach (var match in matches)
        {
            (MatchCompatibility.IsCompatibleWithLevel(match.Match, player?.Level) ? forYou : outOfRange).Add(match);
        }

        string? effectiveCityOrZone = null;
        if (latitude is null || longitude is null)
        {
            effectiveCityOrZone = cityOrZoneOverride ?? player?.CityOrZone;
        }

        // The player's own matches (m5-mobile-confirmation): soonest first, already sorted by the repository.
        var mine = await matchRepository.FindUpcomingConfirmedForPlayerAsync(playerId, clock.GetUtcNow(), cancellationToken);

        return new MatchFeed(
            mine.Where(m => m.Match.Status == MatchStatus.Full).Select(m => new MatchWithDistance(m, DistanceKm: null)).ToList(),
            mine.Where(m => m.Match.Status == MatchStatus.Open).Select(m => new MatchWithDistance(m, DistanceKm: null)).ToList(),
            Order(forYou, latitude, longitude, effectiveCityOrZone),
            Order(outOfRange, latitude, longitude, effectiveCityOrZone));
    }

    private static List<MatchWithDistance> Order(
        List<MatchWithSlotDetails> matches, double? latitude, double? longitude, string? cityOrZone)
    {
        if (latitude is { } lat && longitude is { } lng)
        {
            var withCoordinates = matches
                .Where(m => m.ClubLatitude is not null && m.ClubLongitude is not null)
                .Select(m => new MatchWithDistance(m, HaversineDistanceCalculator.DistanceKm(lat, lng, m.ClubLatitude!.Value, m.ClubLongitude!.Value)))
                .OrderBy(m => m.DistanceKm)
                .ThenBy(m => m.Details.StartsAt);

            var withoutCoordinates = matches
                .Where(m => m.ClubLatitude is null || m.ClubLongitude is null)
                .OrderBy(m => m.StartsAt)
                .Select(m => new MatchWithDistance(m, DistanceKm: null));

            return withCoordinates.Concat(withoutCoordinates).ToList();
        }

        if (!string.IsNullOrWhiteSpace(cityOrZone))
        {
            var matching = matches
                .Where(m => string.Equals(m.ClubCityOrZone, cityOrZone, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.StartsAt);
            var rest = matches
                .Where(m => !string.Equals(m.ClubCityOrZone, cityOrZone, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.StartsAt);

            return matching.Concat(rest).Select(m => new MatchWithDistance(m, DistanceKm: null)).ToList();
        }

        return matches
            .OrderBy(m => m.StartsAt)
            .Select(m => new MatchWithDistance(m, DistanceKm: null))
            .ToList();
    }
}
