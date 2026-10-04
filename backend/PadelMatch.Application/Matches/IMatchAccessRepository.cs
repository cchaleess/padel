using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Application.Matches;

public interface IMatchAccessRepository
{
    Task<MatchAccessRequest?> FindRequestAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    /// <summary>Same as <see cref="FindRequestAsync"/>, but locks the row (SELECT ... FOR UPDATE) until the current
    /// transaction ends, so votes on one request apply one at a time (design.md, "Votar").</summary>
    Task<MatchAccessRequest?> FindRequestForUpdateAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken);

    Task<IAccessTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task AddRequestAsync(MatchAccessRequest request, CancellationToken cancellationToken);

    Task AddVoteAsync(MatchAccessVote vote, CancellationToken cancellationToken);

    Task<IReadOnlyList<MatchAccessVote>> GetVotesAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>Ids of the matches where the player has an Approved request (the feed counts them as joinable).</summary>
    Task<IReadOnlyList<Guid>> GetApprovedMatchIdsAsync(Guid playerId, CancellationToken cancellationToken);

    /// <summary>The match's Pending requests with each requester's profile data and the votes cast so far.</summary>
    Task<IReadOnlyList<PendingAccessRequest>> GetPendingRequestsAsync(Guid matchId, CancellationToken cancellationToken);

    /// <summary>Pending requests on upcoming Open matches where the voter is confirmed and hasn't voted yet.</summary>
    Task<IReadOnlyList<AccessRequestToVote>> GetRequestsToVoteAsync(Guid voterId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>The player's own requests on upcoming matches, any status.</summary>
    Task<IReadOnlyList<OwnAccessRequest>> GetOwnRequestsAsync(Guid playerId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Closes every Pending request of the match as Expired, atomically (UPDATE ... WHERE Status =
    /// 'Pending'). A vote holding the request's row lock finishes first; one arriving later finds it resolved.</summary>
    Task ExpirePendingRequestsAsync(Guid matchId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <exception cref="AccessAlreadyRequestedException">The (MatchId, PlayerId) unique index rejected a request.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IAccessTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

public sealed record AccessRequester(Guid PlayerId, string DisplayName, decimal? Level, int MatchesPlayed);

public sealed record PendingAccessRequest(AccessRequester Requester, int? RequestedPosition, IReadOnlyList<MatchAccessVote> Votes);

public sealed record AccessMatchSummary(
    Guid MatchId, string ClubName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, MatchType Type);

public sealed record AccessRequestToVote(AccessMatchSummary Match, AccessRequester Requester);

public sealed record OwnAccessRequest(AccessMatchSummary Match, AccessRequestStatus Status);
