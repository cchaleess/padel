using PadelMatch.Application.Clubs;
using PadelMatch.Domain.Clubs;

namespace PadelMatch.Api.Clubs;

public sealed record ClubSummaryResponse(Guid Id, string Name, string? CityOrZone, ClubStatus Status, double? DistanceKm)
{
    public static ClubSummaryResponse From(ClubWithDistance club) => new(
        club.Club.Id, club.Club.Name, club.Club.CityOrZone, club.Club.Status, club.DistanceKm);
}

public sealed record CourtResponse(Guid Id, string Name)
{
    public static CourtResponse From(Court court) => new(court.Id, court.Name);
}

public sealed record ClubDetailResponse(
    Guid Id, string Name, string Address, string? CityOrZone, ClubStatus Status, IReadOnlyList<CourtResponse> Courts)
{
    public static ClubDetailResponse From(ClubDetails details) => new(
        details.Club.Id,
        details.Club.Name,
        details.Club.Address,
        details.Club.CityOrZone,
        details.Club.Status,
        details.Courts.Select(CourtResponse.From).ToList());
}

public sealed record CourtSlotResponse(Guid Id, Guid CourtId, string CourtName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int DurationMinutes)
{
    public static CourtSlotResponse From(CourtSlotWithCourtName slot) => new(
        slot.Slot.Id, slot.Slot.CourtId, slot.CourtName, slot.Slot.StartsAt, slot.Slot.EndsAt, (int)slot.Slot.Duration);
}

public sealed record SubmitClubRequest(string Name, string Address, string? CityOrZone);
