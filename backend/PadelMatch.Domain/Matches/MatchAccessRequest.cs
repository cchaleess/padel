namespace PadelMatch.Domain.Matches;

public enum AccessRequestStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>A player outside a competitive match's criteria asking to join anyway (plan §16, "solicitud
/// excepcional"). One per player and match: a rejection is final (m6-quality-rules proposal). Approval only grants
/// permission to join; the player still holds and pays a seat like anyone else.</summary>
public sealed class MatchAccessRequest
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public Guid PlayerId { get; private set; }
    public AccessRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private MatchAccessRequest()
    {
    }

    public static MatchAccessRequest Create(Guid matchId, Guid playerId, DateTimeOffset nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = matchId,
        PlayerId = playerId,
        Status = AccessRequestStatus.Pending,
        CreatedAtUtc = nowUtc
    };

    public void Approve(DateTimeOffset nowUtc) => Resolve(AccessRequestStatus.Approved, nowUtc);

    public void Reject(DateTimeOffset nowUtc) => Resolve(AccessRequestStatus.Rejected, nowUtc);

    private void Resolve(AccessRequestStatus status, DateTimeOffset nowUtc)
    {
        if (Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Only a pending request can be resolved; this one is {Status}.");
        }

        Status = status;
        ResolvedAtUtc = nowUtc;
    }
}

/// <summary>One confirmed player's vote on an access request; one per voter and request. Approval needs every
/// currently confirmed player to approve, and a single rejection rejects (plan §16, unanimity).</summary>
public sealed class MatchAccessVote
{
    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid VoterId { get; private set; }
    public bool Approve { get; private set; }
    public DateTimeOffset CastAtUtc { get; private set; }

    private MatchAccessVote()
    {
    }

    public static MatchAccessVote Cast(Guid requestId, Guid voterId, bool approve, DateTimeOffset nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        RequestId = requestId,
        VoterId = voterId,
        Approve = approve,
        CastAtUtc = nowUtc
    };
}
