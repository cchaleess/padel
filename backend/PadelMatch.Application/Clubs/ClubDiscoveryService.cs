using PadelMatch.Application.Players;
using PadelMatch.Domain.Clubs;

namespace PadelMatch.Application.Clubs;

public sealed class ClubDiscoveryService(
    IClubRepository clubRepository, IPlayerRepository playerRepository, TimeProvider clock) : IClubDiscoveryService
{
    private static readonly TimeSpan DefaultSlotsWindow = TimeSpan.FromDays(14);

    public async Task<IReadOnlyList<ClubWithDistance>> GetNearbyClubsAsync(
        Guid playerId, double? latitude, double? longitude, string? cityOrZoneOverride, CancellationToken cancellationToken)
    {
        var clubs = await clubRepository.GetAllAsync(cancellationToken);

        if (latitude is { } lat && longitude is { } lng)
        {
            return OrderByDistance(clubs, lat, lng);
        }

        var player = await playerRepository.FindByIdAsync(playerId, cancellationToken);
        var effectiveCityOrZone = cityOrZoneOverride ?? player?.CityOrZone;

        return string.IsNullOrWhiteSpace(effectiveCityOrZone)
            ? OrderByName(clubs)
            : OrderByCityOrZoneMatch(clubs, effectiveCityOrZone);
    }

    public async Task<IReadOnlyList<ClubWithDistance>> SearchClubsAsync(string query, CancellationToken cancellationToken)
    {
        var clubs = await clubRepository.GetAllAsync(cancellationToken);

        return clubs
            .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new ClubWithDistance(c, DistanceKm: null))
            .ToList();
    }

    public async Task<ClubDetails> GetClubDetailsAsync(Guid clubId, CancellationToken cancellationToken)
    {
        var club = await clubRepository.FindByIdAsync(clubId, cancellationToken) ?? throw new ClubNotFoundException(clubId);
        var courts = await clubRepository.GetCourtsByClubIdAsync(clubId, cancellationToken);
        return new ClubDetails(club, courts);
    }

    public async Task<IReadOnlyList<CourtSlotWithCourtName>> GetCourtSlotsAsync(
        Guid clubId, Guid? courtId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        _ = await clubRepository.FindByIdAsync(clubId, cancellationToken) ?? throw new ClubNotFoundException(clubId);

        var effectiveFrom = from ?? clock.GetUtcNow();
        var effectiveTo = to ?? effectiveFrom + DefaultSlotsWindow;

        return await clubRepository.GetSlotsAsync(clubId, courtId, effectiveFrom, effectiveTo, cancellationToken);
    }

    private static List<ClubWithDistance> OrderByDistance(IReadOnlyList<Club> clubs, double lat, double lng)
    {
        var withCoordinates = clubs
            .Where(c => c.Latitude is not null && c.Longitude is not null)
            .Select(c => new ClubWithDistance(c, HaversineDistanceCalculator.DistanceKm(lat, lng, c.Latitude!.Value, c.Longitude!.Value)))
            .OrderBy(c => c.DistanceKm);

        var withoutCoordinates = clubs
            .Where(c => c.Latitude is null || c.Longitude is null)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new ClubWithDistance(c, DistanceKm: null));

        return withCoordinates.Concat(withoutCoordinates).ToList();
    }

    private static List<ClubWithDistance> OrderByCityOrZoneMatch(IReadOnlyList<Club> clubs, string cityOrZone)
    {
        var matching = clubs
            .Where(c => string.Equals(c.CityOrZone, cityOrZone, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var rest = clubs
            .Where(c => !string.Equals(c.CityOrZone, cityOrZone, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase);

        return matching.Concat(rest).Select(c => new ClubWithDistance(c, DistanceKm: null)).ToList();
    }

    private static List<ClubWithDistance> OrderByName(IReadOnlyList<Club> clubs) =>
        clubs
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => new ClubWithDistance(c, DistanceKm: null))
            .ToList();
}
