using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

public interface IMatchAccessService
{
    /// <exception cref="MatchNotFoundException"/>
    /// <exception cref="AccessRequestsClosedException">The match is full or has already started.</exception>
    /// <exception cref="PlayerAlreadyHasSeatException"/>
    /// <exception cref="AccessNotNeededException">The player already meets the criteria.</exception>
    /// <exception cref="AccessAlreadyRequestedException"/>
    /// <exception cref="ArgumentOutOfRangeException">The requested position isn't 0–3.</exception>
    Task RequestAccessAsync(Guid matchId, Guid playerId, int? requestedPosition, CancellationToken cancellationToken);

    /// <returns>The request's status after the vote.</returns>
    /// <exception cref="AccessRequestNotFoundException"/>
    /// <exception cref="NotAConfirmedPlayerException"/>
    /// <exception cref="VoteAlreadyCastException">The voter already voted the other way.</exception>
    /// <exception cref="AccessRequestAlreadyResolvedException"/>
    Task<AccessRequestStatus> VoteAsync(
        Guid matchId, Guid requesterId, Guid voterId, bool approve, CancellationToken cancellationToken);

    /// <summary>What the match detail shows about access: the viewer's own situation, and, if they're a confirmed
    /// player, the pending requests they vote on (other players' requests aren't public).</summary>
    Task<MatchAccessView> GetAccessViewAsync(Match match, Guid viewerId, CancellationToken cancellationToken);

    /// <summary>The "Actividad" data: requests the player has to vote on, and their own requests.</summary>
    Task<AccessActivity> GetActivityAsync(Guid playerId, CancellationToken cancellationToken);
}

public sealed record MatchAccessView(
    bool CanJoinDirectly,
    IReadOnlyList<AccessShortfall> Shortfalls,
    AccessRequestStatus? RequestStatus,
    int? RequestedPosition,
    IReadOnlyList<PendingAccessRequestView> PendingRequests);

public sealed record PendingAccessRequestView(
    AccessRequester Requester,
    int? RequestedPosition,
    IReadOnlyList<AccessShortfall> Shortfalls,
    int Approvals,
    int VotersNeeded,
    bool? MyVote);

public sealed record AccessActivity(IReadOnlyList<AccessRequestToVote> ToVote, IReadOnlyList<OwnAccessRequest> MyRequests);
