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
    string? Note)
{
    public static MatchDetailResponse From(MatchWithSlotDetails details) => new(
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
        details.Match.Note);
}

public sealed record CreateMatchRequest(
    Guid CourtSlotId, MatchType Type, decimal? MinLevel, decimal? MaxLevel, int? MinMatchesRequired, string? Note);

public sealed record MatchFeedResponse(IReadOnlyList<MatchFeedItemResponse> ForYou, IReadOnlyList<MatchFeedItemResponse> OutOfRange)
{
    public static MatchFeedResponse From(MatchFeed feed) => new(
        feed.ForYou.Select(MatchFeedItemResponse.From).ToList(),
        feed.OutOfRange.Select(MatchFeedItemResponse.From).ToList());
}

public sealed record MatchFeedItemResponse(
    Guid Id, Guid ClubId, string ClubName, string CourtName,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int DurationMinutes,
    MatchType Type, decimal? MinLevel, decimal? MaxLevel, double? DistanceKm)
{
    public static MatchFeedItemResponse From(MatchWithDistance item) => new(
        item.Details.Match.Id, item.Details.ClubId, item.Details.ClubName, item.Details.CourtName,
        item.Details.StartsAt, item.Details.EndsAt, (int)(item.Details.EndsAt - item.Details.StartsAt).TotalMinutes,
        item.Details.Match.Type, item.Details.Match.MinLevel, item.Details.Match.MaxLevel, item.DistanceKm);
}
