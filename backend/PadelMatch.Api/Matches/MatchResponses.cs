using PadelMatch.Application.Matches;
using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Api.Matches;

public sealed record MatchDetailResponse(
    Guid Id,
    Guid ClubId,
    string ClubName,
    Guid CourtId,
    string CourtName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int DurationMinutes,
    MatchType Type,
    MatchStatus Status,
    Guid OrganizerId,
    decimal? OrganizerLevelAtCreation,
    decimal? MinLevel,
    decimal? MaxLevel,
    int? MinMatchesRequired,
    string? Note,
    int ConfirmedSeats,
    MySeatResponse? MySeat,
    IReadOnlyList<ConfirmedPlayerResponse> ConfirmedPlayers)
{
    public static MatchDetailResponse From(
        MatchWithSlotDetails details, PlayerSeat? mySeat, IReadOnlyList<ConfirmedPlayer> confirmedPlayers) => new(
        details.Match.Id,
        details.ClubId,
        details.ClubName,
        details.CourtId,
        details.CourtName,
        details.StartsAt,
        details.EndsAt,
        (int)(details.EndsAt - details.StartsAt).TotalMinutes,
        details.Match.Type,
        details.Match.Status,
        details.Match.OrganizerId,
        details.Match.OrganizerLevelAtCreation,
        details.Match.MinLevel,
        details.Match.MaxLevel,
        details.Match.MinMatchesRequired,
        details.Match.Note,
        details.ConfirmedSeats,
        mySeat is null ? null : new MySeatResponse(mySeat.Position, mySeat.Status, mySeat.HeldUntilUtc),
        confirmedPlayers.Select(p => new ConfirmedPlayerResponse(p.Position, p.PlayerId, p.DisplayName, p.Level)).ToList());
}

/// <summary>Position 0–3: 0–1 are pair A, 2–3 pair B.</summary>
public sealed record ConfirmedPlayerResponse(int Position, Guid PlayerId, string DisplayName, decimal? Level);

/// <summary>Optional body for hold: the seat (0–3) the player picked; omitted means any free seat.</summary>
public sealed record HoldSeatRequest(int? Position);

public sealed record MySeatResponse(int Position, SeatStatus Status, DateTimeOffset? HeldUntilUtc);

public sealed record CreateMatchRequest(
    Guid CourtSlotId, MatchType Type, decimal? MinLevel, decimal? MaxLevel, int? MinMatchesRequired, string? Note);

public sealed record SeatHoldResponse(DateTimeOffset HeldUntilUtc);

public sealed record MatchFeedResponse(
    IReadOnlyList<MatchFeedItemResponse> Confirmed,
    IReadOnlyList<MatchFeedItemResponse> PendingConfirmation,
    IReadOnlyList<MatchFeedItemResponse> ForYou,
    IReadOnlyList<MatchFeedItemResponse> OutOfRange)
{
    public static MatchFeedResponse From(MatchFeed feed) => new(
        feed.Confirmed.Select(MatchFeedItemResponse.From).ToList(),
        feed.PendingConfirmation.Select(MatchFeedItemResponse.From).ToList(),
        feed.ForYou.Select(MatchFeedItemResponse.From).ToList(),
        feed.OutOfRange.Select(MatchFeedItemResponse.From).ToList());
}

public sealed record MatchFeedItemResponse(
    Guid Id, Guid ClubId, string ClubName, string CourtName,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int DurationMinutes,
    MatchType Type, decimal? MinLevel, decimal? MaxLevel, double? DistanceKm, int ConfirmedSeats)
{
    public static MatchFeedItemResponse From(MatchWithDistance item) => new(
        item.Details.Match.Id, item.Details.ClubId, item.Details.ClubName, item.Details.CourtName,
        item.Details.StartsAt, item.Details.EndsAt, (int)(item.Details.EndsAt - item.Details.StartsAt).TotalMinutes,
        item.Details.Match.Type, item.Details.Match.MinLevel, item.Details.Match.MaxLevel, item.DistanceKm,
        item.Details.ConfirmedSeats);
}
