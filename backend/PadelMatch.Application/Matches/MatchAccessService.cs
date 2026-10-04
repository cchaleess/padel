using PadelMatch.Application.Players;
using PadelMatch.Domain.Matches;

namespace PadelMatch.Application.Matches;

/// <summary>Exceptional access to competitive matches (plan §16, m6-quality-rules): a player outside the
/// criteria requests it, and every currently confirmed player has to approve.</summary>
public sealed class MatchAccessService(
    IMatchAccessRepository accessRepository,
    IMatchRepository matchRepository,
    IMatchSeatRepository seatRepository,
    IPlayerRepository playerRepository,
    TimeProvider clock) : IMatchAccessService
{
    public async Task RequestAccessAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var details = await matchRepository.FindDetailsByIdAsync(matchId, cancellationToken)
            ?? throw new MatchNotFoundException(matchId);

        if (details.Match.Status != MatchStatus.Open || details.StartsAt <= now)
        {
            throw new AccessRequestsClosedException();
        }

        if (await seatRepository.HasActiveSeatAsync(matchId, playerId, now, cancellationToken))
        {
            throw new PlayerAlreadyHasSeatException();
        }

        var player = await playerRepository.FindByIdAsync(playerId, cancellationToken);
        if (MatchCompatibility.CanJoinDirectly(details.Match, player?.Level, player?.MatchesPlayed ?? 0))
        {
            throw new AccessNotNeededException();
        }

        if (await accessRepository.FindRequestAsync(matchId, playerId, cancellationToken) is not null)
        {
            throw new AccessAlreadyRequestedException();
        }

        await accessRepository.AddRequestAsync(MatchAccessRequest.Create(matchId, playerId, now), cancellationToken);
        await accessRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccessRequestStatus> VoteAsync(
        Guid matchId, Guid requesterId, Guid voterId, bool approve, CancellationToken cancellationToken)
    {
        // The row lock serializes votes on this request: without it, the last two approvals arriving together
        // could each see only their own vote and leave the request pending forever (design.md, "Votar").
        await using var transaction = await accessRepository.BeginTransactionAsync(cancellationToken);

        var request = await accessRepository.FindRequestForUpdateAsync(matchId, requesterId, cancellationToken)
            ?? throw new AccessRequestNotFoundException();

        var confirmedIds = (await seatRepository.GetConfirmedPlayersAsync(matchId, cancellationToken))
            .Select(p => p.PlayerId)
            .ToHashSet();
        if (!confirmedIds.Contains(voterId))
        {
            throw new NotAConfirmedPlayerException();
        }

        var votes = await accessRepository.GetVotesAsync(request.Id, cancellationToken);
        if (votes.FirstOrDefault(v => v.VoterId == voterId) is { } previous)
        {
            // Repeating the same vote is a no-op (idempotent transitions, constitution); changing it isn't allowed.
            return previous.Approve == approve ? request.Status : throw new VoteAlreadyCastException();
        }

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new AccessRequestAlreadyResolvedException();
        }

        var now = clock.GetUtcNow();
        await accessRepository.AddVoteAsync(MatchAccessVote.Cast(request.Id, voterId, approve, now), cancellationToken);

        if (!approve)
        {
            request.Reject(now);
        }
        else
        {
            var approvers = votes.Where(v => v.Approve).Select(v => v.VoterId).Append(voterId).ToHashSet();
            if (confirmedIds.IsSubsetOf(approvers))
            {
                request.Approve(now);
            }
        }

        await accessRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return request.Status;
    }

    public async Task<MatchAccessView> GetAccessViewAsync(Match match, Guid viewerId, CancellationToken cancellationToken)
    {
        var viewer = await playerRepository.FindByIdAsync(viewerId, cancellationToken);
        var shortfalls = MatchCompatibility.GetShortfalls(match, viewer?.Level, viewer?.MatchesPlayed ?? 0);
        var ownRequest = await accessRepository.FindRequestAsync(match.Id, viewerId, cancellationToken);

        var confirmedIds = (await seatRepository.GetConfirmedPlayersAsync(match.Id, cancellationToken))
            .Select(p => p.PlayerId)
            .ToHashSet();

        IReadOnlyList<PendingAccessRequestView> pending = [];
        if (confirmedIds.Contains(viewerId))
        {
            pending = (await accessRepository.GetPendingRequestsAsync(match.Id, cancellationToken))
                .Select(r => new PendingAccessRequestView(
                    r.Requester,
                    MatchCompatibility.GetShortfalls(match, r.Requester.Level, r.Requester.MatchesPlayed),
                    r.Votes.Count(v => v.Approve && confirmedIds.Contains(v.VoterId)),
                    confirmedIds.Count,
                    r.Votes.FirstOrDefault(v => v.VoterId == viewerId)?.Approve))
                .ToList();
        }

        return new MatchAccessView(shortfalls.Count == 0, shortfalls, ownRequest?.Status, pending);
    }

    public async Task<AccessActivity> GetActivityAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return new AccessActivity(
            await accessRepository.GetRequestsToVoteAsync(playerId, now, cancellationToken),
            await accessRepository.GetOwnRequestsAsync(playerId, now, cancellationToken));
    }
}
