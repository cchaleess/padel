using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PadelMatch.Application.Matches;
using PadelMatch.Domain.Matches;
// MatchType also exists in System.IO (implicit usings); alias resolves the ambiguity.
using MatchType = PadelMatch.Domain.Matches.MatchType;

namespace PadelMatch.Infrastructure.Persistence;

internal sealed class MatchAccessRepository(PadelMatchDbContext dbContext) : IMatchAccessRepository
{
    public Task<MatchAccessRequest?> FindRequestAsync(Guid matchId, Guid playerId, CancellationToken cancellationToken) =>
        dbContext.MatchAccessRequests.FirstOrDefaultAsync(r => r.MatchId == matchId && r.PlayerId == playerId, cancellationToken);

    public async Task<MatchAccessRequest?> FindRequestForUpdateAsync(
        Guid matchId, Guid playerId, CancellationToken cancellationToken) =>
        // ToListAsync, not FirstOrDefaultAsync: composing on top of FromSql would wrap it in a subquery, and
        // PostgreSQL doesn't allow FOR UPDATE there.
        (await dbContext.MatchAccessRequests
            .FromSql($"""SELECT * FROM "MatchAccessRequests" WHERE "MatchId" = {matchId} AND "PlayerId" = {playerId} FOR UPDATE""")
            .ToListAsync(cancellationToken))
        .FirstOrDefault();

    public async Task<IAccessTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfAccessTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));

    public async Task AddRequestAsync(MatchAccessRequest request, CancellationToken cancellationToken) =>
        await dbContext.MatchAccessRequests.AddAsync(request, cancellationToken);

    public async Task AddVoteAsync(MatchAccessVote vote, CancellationToken cancellationToken) =>
        await dbContext.MatchAccessVotes.AddAsync(vote, cancellationToken);

    public async Task<IReadOnlyList<MatchAccessVote>> GetVotesAsync(Guid requestId, CancellationToken cancellationToken) =>
        await dbContext.MatchAccessVotes.Where(v => v.RequestId == requestId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetApprovedMatchIdsAsync(Guid playerId, CancellationToken cancellationToken) =>
        await dbContext.MatchAccessRequests
            .Where(r => r.PlayerId == playerId && r.Status == AccessRequestStatus.Approved)
            .Select(r => r.MatchId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PendingAccessRequest>> GetPendingRequestsAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var requests = await dbContext.MatchAccessRequests
            .Where(r => r.MatchId == matchId && r.Status == AccessRequestStatus.Pending)
            .Join(dbContext.Players, r => r.PlayerId, p => p.Id, (r, p) => new
            {
                r.Id, r.CreatedAtUtc, r.RequestedPosition, p.DisplayName, PlayerId = p.Id, p.Level, p.MatchesPlayed
            })
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var requestIds = requests.Select(r => r.Id).ToList();
        var votes = await dbContext.MatchAccessVotes.Where(v => requestIds.Contains(v.RequestId)).ToListAsync(cancellationToken);

        return requests
            .Select(r => new PendingAccessRequest(
                new AccessRequester(r.PlayerId, r.DisplayName, r.Level, r.MatchesPlayed),
                r.RequestedPosition,
                votes.Where(v => v.RequestId == r.Id).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<AccessRequestToVote>> GetRequestsToVoteAsync(
        Guid voterId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await MatchSummaries(dbContext.MatchAccessRequests
                .Where(r => r.Status == AccessRequestStatus.Pending &&
                            dbContext.MatchSeats.Any(s => s.MatchId == r.MatchId && s.HolderId == voterId && s.Status == SeatStatus.Confirmed) &&
                            !dbContext.MatchAccessVotes.Any(v => v.RequestId == r.Id && v.VoterId == voterId)),
                now, openOnly: true)
            .Join(dbContext.Players, x => x.Request.PlayerId, p => p.Id, (x, p) => new
            {
                x.MatchId, x.ClubName, x.StartsAt, x.EndsAt, x.Type, PlayerId = p.Id, p.DisplayName, p.Level, p.MatchesPlayed
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.StartsAt)
            .Select(x => new AccessRequestToVote(
                new AccessMatchSummary(x.MatchId, x.ClubName, x.StartsAt, x.EndsAt, x.Type),
                new AccessRequester(x.PlayerId, x.DisplayName, x.Level, x.MatchesPlayed)))
            .ToList();
    }

    public async Task<IReadOnlyList<OwnAccessRequest>> GetOwnRequestsAsync(
        Guid playerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await MatchSummaries(dbContext.MatchAccessRequests.Where(r => r.PlayerId == playerId), now, openOnly: false)
            .Select(x => new { x.MatchId, x.ClubName, x.StartsAt, x.EndsAt, x.Type, x.Request.Status })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(x => x.StartsAt)
            .Select(x => new OwnAccessRequest(new AccessMatchSummary(x.MatchId, x.ClubName, x.StartsAt, x.EndsAt, x.Type), x.Status))
            .ToList();
    }

    public async Task ExpirePendingRequestsAsync(Guid matchId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await dbContext.MatchAccessRequests
            .Where(r => r.MatchId == matchId && r.Status == AccessRequestStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, AccessRequestStatus.Expired)
                    .SetProperty(r => r.ResolvedAtUtc, now),
                cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation,
                                               ConstraintName: "IX_MatchAccessRequests_MatchId_PlayerId"
                                           })
        {
            throw new AccessAlreadyRequestedException();
        }
    }

    /// <summary>Joins requests to their match's club and schedule, keeping only upcoming matches (and Open ones if
    /// asked). Anonymous shapes throughout: EF can't filter or order on an already-projected record.</summary>
    private IQueryable<RequestWithMatch> MatchSummaries(IQueryable<MatchAccessRequest> requests, DateTimeOffset now, bool openOnly) =>
        requests
            .Join(dbContext.Matches, r => r.MatchId, m => m.Id, (r, m) => new { r, m })
            .Where(x => !openOnly || x.m.Status == MatchStatus.Open)
            .Join(dbContext.CourtSlots, x => x.m.CourtSlotId, s => s.Id, (x, s) => new { x.r, x.m, s })
            .Where(x => x.s.StartsAt > now)
            .Join(dbContext.Courts, x => x.s.CourtId, c => c.Id, (x, c) => new { x.r, x.m, x.s, c })
            .Join(dbContext.Clubs, x => x.c.ClubId, club => club.Id, (x, club) => new RequestWithMatch
            {
                Request = x.r,
                MatchId = x.m.Id,
                ClubName = club.Name,
                StartsAt = x.s.StartsAt,
                EndsAt = x.s.EndsAt,
                Type = x.m.Type
            });

    private sealed class RequestWithMatch
    {
        public required MatchAccessRequest Request { get; init; }
        public Guid MatchId { get; init; }
        public required string ClubName { get; init; }
        public DateTimeOffset StartsAt { get; init; }
        public DateTimeOffset EndsAt { get; init; }
        public MatchType Type { get; init; }
    }

    private sealed class EfAccessTransaction(IDbContextTransaction transaction) : IAccessTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
