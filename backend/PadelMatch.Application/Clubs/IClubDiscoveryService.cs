using PadelMatch.Domain.Clubs;

namespace PadelMatch.Application.Clubs;

public interface IClubDiscoveryService
{
    /// <summary>Orders by distance when <paramref name="latitude"/>/<paramref name="longitude"/> are given;
    /// otherwise falls back to <paramref name="cityOrZoneOverride"/> or the player's own profile
    /// <c>CityOrZone</c>; with no signal at all, orders by name. Never fails for lack of a location signal.</summary>
    Task<IReadOnlyList<ClubWithDistance>> GetNearbyClubsAsync(
        Guid playerId, double? latitude, double? longitude, string? cityOrZoneOverride, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClubWithDistance>> SearchClubsAsync(string query, CancellationToken cancellationToken);

    /// <exception cref="ClubNotFoundException" />
    Task<ClubDetails> GetClubDetailsAsync(Guid clubId, CancellationToken cancellationToken);

    /// <exception cref="ClubNotFoundException" />
    Task<IReadOnlyList<CourtSlotWithCourtName>> GetCourtSlotsAsync(
        Guid clubId, Guid? courtId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
}

public sealed record ClubWithDistance(Club Club, double? DistanceKm);

public sealed record ClubDetails(Club Club, IReadOnlyList<Court> Courts);
