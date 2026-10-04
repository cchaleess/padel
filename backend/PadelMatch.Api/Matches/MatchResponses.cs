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
    IReadOnlyList<ConfirmedPlayerResponse> ConfirmedPlayers,
    MyAccessResponse MyAccess,
    IReadOnlyList<PendingAccessRequestResponse> PendingRequests)
{
    public static MatchDetailResponse From(
        MatchWithSlotDetails details, PlayerSeat? mySeat, IReadOnlyList<ConfirmedPlayer> confirmedPlayers,
        MatchAccessView access) => new(
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
        confirmedPlayers.Select(p => new ConfirmedPlayerResponse(p.Position, p.PlayerId, p.DisplayName, p.Level)).ToList(),
        new MyAccessResponse(access.CanJoinDirectly, access.Shortfalls, access.RequestStatus, access.RequestedPosition),
        access.PendingRequests.Select(r => new PendingAccessRequestResponse(
            AccessRequesterResponse.From(r.Requester), r.RequestedPosition, r.Shortfalls, r.Approvals, r.VotersNeeded, r.MyVote)).ToList());
}

/// <summary>Whether the caller can hold a seat directly (m6-quality-rules) and, if not, why and how their
/// exception request stands.</summary>
public sealed record MyAccessResponse(
    bool CanJoinDirectly, IReadOnlyList<AccessShortfall> Shortfalls, AccessRequestStatus? RequestStatus,
    int? RequestedPosition);

/// <summary>Optional body for an exception request: the seat (0–3) the player asked from.</summary>
public sealed record AccessRequestBody(int? Position);

/// <summary>Only filled for confirmed players, who are the ones voting.</summary>
public sealed record PendingAccessRequestResponse(
    AccessRequesterResponse Requester,
    int? RequestedPosition,
    IReadOnlyList<AccessShortfall> Shortfalls,
    int Approvals,
    int VotersNeeded,
    bool? MyVote);

public sealed record AccessRequesterResponse(Guid PlayerId, string DisplayName, decimal? Level, int MatchesPlayed)
{
    public static AccessRequesterResponse From(AccessRequester r) => new(r.PlayerId, r.DisplayName, r.Level, r.MatchesPlayed);
}

public sealed record AccessVoteResponse(AccessRequestStatus Status);

public sealed record ActivityResponse(
    IReadOnlyList<RequestToVoteResponse> ToVote, IReadOnlyList<OwnRequestResponse> MyRequests)
{
    public static ActivityResponse From(AccessActivity activity) => new(
        activity.ToVote.Select(t => new RequestToVoteResponse(
            ActivityMatchResponse.From(t.Match), AccessRequesterResponse.From(t.Requester))).ToList(),
        activity.MyRequests.Select(r => new OwnRequestResponse(ActivityMatchResponse.From(r.Match), r.Status)).ToList());
}

public sealed record ActivityMatchResponse(
    Guid MatchId, string ClubName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, MatchType Type)
{
    public static ActivityMatchResponse From(AccessMatchSummary m) => new(m.MatchId, m.ClubName, m.StartsAt, m.EndsAt, m.Type);
}

public sealed record RequestToVoteResponse(ActivityMatchResponse Match, AccessRequesterResponse Requester);

public sealed record OwnRequestResponse(ActivityMatchResponse Match, AccessRequestStatus Status);

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
